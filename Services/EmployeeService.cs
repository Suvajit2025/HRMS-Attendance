using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using HRMS.ViewModels;

namespace HRMS.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IFaceRecognitionService _faceService;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IEmployeeRepository employeeRepo,
        IDepartmentRepository departmentRepo,
        IFaceRecognitionService faceService,
        StoragePathProvider pathProvider,
        ILogger<EmployeeService> logger)
    {
        _employeeRepo = employeeRepo;
        _departmentRepo = departmentRepo;
        _faceService = faceService;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<EmployeeListViewModel> GetPagedEmployeesAsync(
        string? searchTerm,
        string? departmentId,
        string? statusFilter,
        int page,
        int pageSize)
    {
        var all = await _employeeRepo.GetAllAsync();
        var departments = await _departmentRepo.GetAllAsync();
        var deptDict = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        var query = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(e =>
                e.EmployeeNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.PhoneNo != null && e.PhoneNo.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            query = query.Where(e => string.Equals(e.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (string.Equals(statusFilter, "active", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.IsActive);
            else if (string.Equals(statusFilter, "inactive", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => !e.IsActive);
        }

        var filteredList = query.ToList();
        var totalCount = filteredList.Count;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var pagedItems = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e =>
            {
                if (!string.IsNullOrEmpty(e.DepartmentId) && deptDict.TryGetValue(e.DepartmentId, out var dn))
                {
                    e.DepartmentName = dn;
                }
                return e;
            })
            .ToList();

        return new EmployeeListViewModel
        {
            Employees = pagedItems,
            SearchTerm = searchTerm,
            DepartmentId = departmentId,
            StatusFilter = statusFilter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Departments = departments.Where(d => d.IsActive).ToList()
        };
    }

    public async Task<EmployeeModel?> GetEmployeeByIdAsync(string employeeId)
    {
        var emp = await _employeeRepo.GetByIdAsync(employeeId);
        if (emp != null && !string.IsNullOrEmpty(emp.DepartmentId))
        {
            var dept = await _departmentRepo.GetByIdAsync(emp.DepartmentId);
            if (dept != null) emp.DepartmentName = dept.DepartmentName;
        }
        return emp;
    }

    public async Task<EmployeeModel?> GetEmployeeByNoAsync(string employeeNo)
    {
        var emp = await _employeeRepo.GetByEmployeeNoAsync(employeeNo);
        if (emp != null && !string.IsNullOrEmpty(emp.DepartmentId))
        {
            var dept = await _departmentRepo.GetByIdAsync(emp.DepartmentId);
            if (dept != null) emp.DepartmentName = dept.DepartmentName;
        }
        return emp;
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateEmployeeAsync(EmployeeEditViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.EmployeeNo))
            return (false, "Employee Number is required.");

        if (await _employeeRepo.ExistsEmployeeNoAsync(model.EmployeeNo.Trim()))
            return (false, $"Employee Number '{model.EmployeeNo}' is already assigned to another employee.");

        if (string.IsNullOrWhiteSpace(model.FirstName))
            return (false, "First Name is required.");

        if (string.IsNullOrWhiteSpace(model.LastName))
            return (false, "Last Name is required.");

        if (string.IsNullOrWhiteSpace(model.AadhaarNo) || model.AadhaarNo.Trim().Length != 12 || !model.AadhaarNo.All(char.IsDigit))
            return (false, "Aadhaar number must contain exactly 12 numeric digits.");

        string? photoPath = null;
        if (model.PhotoFile != null || !string.IsNullOrWhiteSpace(model.CameraPhotoBase64))
        {
            photoPath = await SaveEmployeePhotoAsync(model.PhotoFile, model.CameraPhotoBase64, model.EmployeeNo);
        }

        string? deptName = null;
        if (!string.IsNullOrEmpty(model.DepartmentId))
        {
            var dept = await _departmentRepo.GetByIdAsync(model.DepartmentId);
            deptName = dept?.DepartmentName;
        }

        var employee = new EmployeeModel
        {
            EmployeeId = Guid.NewGuid().ToString(),
            EmployeeNo = model.EmployeeNo.Trim().ToUpperInvariant(),
            FirstName = model.FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim(),
            LastName = model.LastName.Trim(),
            DOB = model.DOB,
            JoiningDate = model.JoiningDate,
            AadhaarNo = model.AadhaarNo.Trim(),
            PhoneNo = string.IsNullOrWhiteSpace(model.PhoneNo) ? null : model.PhoneNo.Trim(),
            Gender = model.Gender,
            DepartmentId = model.DepartmentId,
            DepartmentName = deptName,
            IsActive = model.IsActive,
            Photo = photoPath,
            CreatedDate = DateTime.UtcNow
        };

        await _employeeRepo.AddAsync(employee);
        _logger.LogInformation("Created new employee {EmployeeNo} with ID {EmployeeId}", employee.EmployeeNo, employee.EmployeeId);

        if (!string.IsNullOrEmpty(photoPath))
        {
            await TryEnrollPhotoAsync(employee.EmployeeId, photoPath, model.CameraPhotoBase64);
        }

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateEmployeeAsync(EmployeeEditViewModel model)
    {
        if (string.IsNullOrEmpty(model.EmployeeId))
            return (false, "Invalid employee identifier.");

        var existing = await _employeeRepo.GetByIdAsync(model.EmployeeId);
        if (existing == null)
            return (false, "Employee not found.");

        if (await _employeeRepo.ExistsEmployeeNoAsync(model.EmployeeNo.Trim(), model.EmployeeId))
            return (false, $"Employee Number '{model.EmployeeNo}' is already assigned to another employee.");

        if (string.IsNullOrWhiteSpace(model.FirstName))
            return (false, "First Name is required.");

        if (string.IsNullOrWhiteSpace(model.LastName))
            return (false, "Last Name is required.");

        if (string.IsNullOrWhiteSpace(model.AadhaarNo) || model.AadhaarNo.Trim().Length != 12 || !model.AadhaarNo.All(char.IsDigit))
            return (false, "Aadhaar number must contain exactly 12 numeric digits.");

        string? photoPath = existing.Photo;
        bool photoChanged = false;
        if (model.PhotoFile != null || !string.IsNullOrWhiteSpace(model.CameraPhotoBase64))
        {
            var newPhoto = await SaveEmployeePhotoAsync(model.PhotoFile, model.CameraPhotoBase64, model.EmployeeNo);
            if (!string.IsNullOrEmpty(newPhoto))
            {
                photoPath = newPhoto;
                photoChanged = true;
            }
        }

        string? deptName = null;
        if (!string.IsNullOrEmpty(model.DepartmentId))
        {
            var dept = await _departmentRepo.GetByIdAsync(model.DepartmentId);
            deptName = dept?.DepartmentName;
        }

        existing.EmployeeNo = model.EmployeeNo.Trim().ToUpperInvariant();
        existing.FirstName = model.FirstName.Trim();
        existing.MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim();
        existing.LastName = model.LastName.Trim();
        existing.DOB = model.DOB;
        existing.JoiningDate = model.JoiningDate;
        existing.AadhaarNo = model.AadhaarNo.Trim();
        existing.PhoneNo = string.IsNullOrWhiteSpace(model.PhoneNo) ? null : model.PhoneNo.Trim();
        existing.Gender = model.Gender;
        existing.DepartmentId = model.DepartmentId;
        existing.DepartmentName = deptName;
        existing.IsActive = model.IsActive;
        existing.Photo = photoPath;
        existing.UpdatedDate = DateTime.UtcNow;

        await _employeeRepo.UpdateAsync(existing);
        _logger.LogInformation("Updated employee {EmployeeNo} with ID {EmployeeId}", existing.EmployeeNo, existing.EmployeeId);

        // Auto-enroll biometric face template if a new photo was uploaded/captured or if no template exists yet
        if (!string.IsNullOrEmpty(photoPath))
        {
            var hasTemplate = await _faceService.HasRegisteredFaceAsync(existing.EmployeeId);
            if (photoChanged || !hasTemplate)
            {
                await TryEnrollPhotoAsync(existing.EmployeeId, photoPath, model.CameraPhotoBase64);
            }
        }

        return (true, null);
    }

    private async Task TryEnrollPhotoAsync(string employeeId, string photoPath, string? directBase64)
    {
        try
        {
            string? b64 = directBase64;
            if (string.IsNullOrWhiteSpace(b64))
            {
                var diskPath = Path.Combine(_pathProvider.EmployeeUploadsDirectory, Path.GetFileName(photoPath));
                if (File.Exists(diskPath))
                {
                    var bytes = await File.ReadAllBytesAsync(diskPath);
                    b64 = Convert.ToBase64String(bytes);
                }
            }

            if (!string.IsNullOrWhiteSpace(b64))
            {
                var (ok, msg, tmpl) = await _faceService.RegisterFaceAsync(employeeId, b64);
                if (ok)
                {
                    _logger.LogInformation("Auto-enrolled face biometric for employee {EmployeeId}", employeeId);
                }
                else
                {
                    _logger.LogWarning("Face biometric auto-enroll notice for employee {EmployeeId}: {Msg}", employeeId, msg);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-enroll biometric face for employee {EmployeeId}", employeeId);
        }
    }

    public async Task<bool> ToggleEmployeeStatusAsync(string employeeId)
    {
        var existing = await _employeeRepo.GetByIdAsync(employeeId);
        if (existing == null) return false;

        existing.IsActive = !existing.IsActive;
        existing.UpdatedDate = DateTime.UtcNow;
        await _employeeRepo.UpdateAsync(existing);
        _logger.LogInformation("Toggled active status of employee {EmployeeNo} to {IsActive}", existing.EmployeeNo, existing.IsActive);
        return true;
    }

    public async Task<IReadOnlyList<DepartmentModel>> GetActiveDepartmentsAsync()
    {
        var all = await _departmentRepo.GetAllAsync();
        return all.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
    }

    public async Task<string> GetNextEmployeeNoAsync()
    {
        var all = await _employeeRepo.GetAllAsync();
        int maxNum = 0;
        foreach (var emp in all)
        {
            if (string.IsNullOrWhiteSpace(emp.EmployeeNo)) continue;
            var digits = new string(emp.EmployeeNo.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int num))
            {
                if (num > maxNum) maxNum = num;
            }
        }
        return $"EMP{(maxNum + 1):D3}";
    }

    public async Task<string?> SaveEmployeePhotoAsync(IFormFile? file, string? base64Image, string employeeNo)
    {
        try
        {
            var sanitizedNo = string.Concat(employeeNo.Where(char.IsLetterOrDigit)).ToLowerInvariant();
            var fileName = $"emp_{sanitizedNo}_{Guid.NewGuid():N}.jpg";
            var filePath = Path.Combine(_pathProvider.EmployeeUploadsDirectory, fileName);

            if (file != null && file.Length > 0)
            {
                // Max 5 MB check
                if (file.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("Uploaded file exceeded 5MB limit for employee {EmployeeNo}", employeeNo);
                    return null;
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("Invalid file extension {Extension} for employee photo", ext);
                    return null;
                }

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                return $"/uploads/employees/{fileName}";
            }

            if (!string.IsNullOrWhiteSpace(base64Image))
            {
                var cleanBase64 = base64Image;
                if (cleanBase64.Contains(','))
                {
                    cleanBase64 = cleanBase64.Substring(cleanBase64.IndexOf(',') + 1);
                }

                var bytes = Convert.FromBase64String(cleanBase64);
                if (bytes.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("Base64 photo exceeded 5MB limit for employee {EmployeeNo}", employeeNo);
                    return null;
                }

                await File.WriteAllBytesAsync(filePath, bytes);
                return $"/uploads/employees/{fileName}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save employee photo for {EmployeeNo}", employeeNo);
        }

        return null;
    }
}

