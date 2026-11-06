using HRMS.Interfaces;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class FaceRegistrationController : Controller
{
    private readonly IFaceRecognitionService _faceService;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IFaceTemplateRepository _faceTemplateRepo;
    private readonly ILogger<FaceRegistrationController> _logger;

    public FaceRegistrationController(
        IFaceRecognitionService faceService,
        IEmployeeRepository employeeRepo,
        IDepartmentRepository departmentRepo,
        IFaceTemplateRepository faceTemplateRepo,
        ILogger<FaceRegistrationController> logger)
    {
        _faceService = faceService;
        _employeeRepo = employeeRepo;
        _departmentRepo = departmentRepo;
        _faceTemplateRepo = faceTemplateRepo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Auto-sync any existing profile photos into biometric templates
        await _faceService.SyncEmployeePhotoTemplatesAsync();

        var allEmployees = await _employeeRepo.GetAllAsync();
        var departments = await _departmentRepo.GetAllAsync();
        var deptDict = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);
        var templates = await _faceTemplateRepo.GetAllAsync();
        var templateDict = templates.ToDictionary(t => t.EmployeeId, t => t, StringComparer.OrdinalIgnoreCase);

        var list = allEmployees.Where(e => e.IsActive).Select(e =>
        {
            var hasTemplate = templateDict.TryGetValue(e.EmployeeId, out var tmpl) || (!string.IsNullOrEmpty(e.EmployeeNo) && templates.Any(t => string.Equals(t.EmployeeNo, e.EmployeeNo, StringComparison.OrdinalIgnoreCase)));
            var deptName = !string.IsNullOrEmpty(e.DepartmentId) && deptDict.TryGetValue(e.DepartmentId, out var dn)
                ? dn
                : (e.DepartmentName ?? "Unassigned");

            return new EmployeeRegistrationItem
            {
                EmployeeId = e.EmployeeId,
                EmployeeNo = e.EmployeeNo,
                FullName = e.FullName,
                DepartmentName = deptName,
                PhotoPath = e.Photo,
                HasFaceTemplate = hasTemplate,
                EnrolledDate = tmpl?.EnrolledAt
            };
        }).OrderBy(e => e.EmployeeNo).ToList();

        var model = new FaceRegistrationViewModel
        {
            Employees = list,
            TotalEmployees = list.Count,
            EnrolledCount = list.Count(e => e.HasFaceTemplate),
            ModelInfo = _faceService.GetModelInformation()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnrollFromProfilePhoto(string employeeId)
    {
        if (string.IsNullOrEmpty(employeeId))
        {
            return Json(new { success = false, message = "Employee ID is required." });
        }

        var employee = await _employeeRepo.GetByIdAsync(employeeId) ?? await _employeeRepo.GetByEmployeeNoAsync(employeeId);
        if (employee == null || string.IsNullOrWhiteSpace(employee.Photo))
        {
            return Json(new { success = false, message = "Employee has no profile photo to enroll." });
        }

        await _faceService.SyncEmployeePhotoTemplatesAsync();
        var hasFace = await _faceService.HasRegisteredFaceAsync(employee.EmployeeId);
        if (hasFace)
        {
            return Json(new { success = true, message = $"Face template enrolled successfully from {employee.FullName}'s profile photo!" });
        }

        return Json(new { success = false, message = "Could not detect a clear face in the profile photo. Please upload a clear front-facing photo or use the webcam." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll([FromBody] FaceEnrollmentRequestDto request)
    {
        if (string.IsNullOrEmpty(request.EmployeeId))
        {
            return Json(new FaceEnrollmentResponseDto
            {
                Success = false,
                Message = "Please select an employee to enroll."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return Json(new FaceEnrollmentResponseDto
            {
                Success = false,
                Message = "Camera frame was empty or missing."
            });
        }

        var (success, message, template) = await _faceService.RegisterFaceAsync(request.EmployeeId, request.ImageBase64);

        return Json(new FaceEnrollmentResponseDto
        {
            Success = success,
            Message = message,
            ModelVersion = template?.ModelVersion,
            FaceDetected = success
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string employeeId)
    {
        if (string.IsNullOrEmpty(employeeId))
        {
            return Json(new { success = false, message = "Employee ID is required." });
        }

        await _faceService.RemoveTemplateAsync(employeeId);
        _logger.LogInformation("Removed face template for employee ID {EmployeeId}", employeeId);
        return Json(new { success = true, message = "Face template removed successfully." });
    }
}

