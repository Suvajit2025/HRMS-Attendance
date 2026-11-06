using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using HRMS.ViewModels;
using Microsoft.Extensions.Options;

namespace HRMS.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IFaceTemplateRepository _faceTemplateRepo;
    private readonly StoragePathProvider _pathProvider;
    private readonly JsonStorageOptions _options;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IAttendanceRepository attendanceRepo,
        IEmployeeRepository employeeRepo,
        IDepartmentRepository departmentRepo,
        IFaceTemplateRepository faceTemplateRepo,
        StoragePathProvider pathProvider,
        IOptions<JsonStorageOptions> options,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepo = attendanceRepo;
        _employeeRepo = employeeRepo;
        _departmentRepo = departmentRepo;
        _faceTemplateRepo = faceTemplateRepo;
        _pathProvider = pathProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AttendanceModel? Punch)> ProcessPunchAsync(
        string employeeId,
        string requestedPunchType,
        string verificationMethod = "Manual",
        float? matchScore = null,
        string? capturedPhotoBase64 = null)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId);
        if (employee == null)
        {
            return (false, "Employee not recognized in the system.", null);
        }

        if (!employee.IsActive)
        {
            return (false, $"Attendance rejected: Employee {employee.FullName} ({employee.EmployeeNo}) is inactive.", null);
        }

        if (string.Equals(verificationMethod, "FaceRecognition", StringComparison.OrdinalIgnoreCase))
        {
            var template = await _faceTemplateRepo.GetByEmployeeIdAsync(employeeId);
            if (template == null)
            {
                return (false, $"Attendance rejected: Face template is not registered for {employee.FullName}. Please enroll your face first.", null);
            }
        }

        // Determine server-side time in configured timezone
        var serverNowUtc = DateTime.UtcNow;
        DateTime localPunchTime = serverNowUtc;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
            localPunchTime = TimeZoneInfo.ConvertTimeFromUtc(serverNowUtc, tz);
        }
        catch
        {
            localPunchTime = DateTime.Now;
        }

        var today = DateOnly.FromDateTime(localPunchTime);
        var existingPunches = await _attendanceRepo.GetEmployeePunchesTodayAsync(employeeId, today);

        var normType = requestedPunchType.Trim().ToUpperInvariant();
        if (normType != "IN" && normType != "OUT")
        {
            return (false, "Invalid punch type requested. Must be 'IN' or 'OUT'.", null);
        }

        bool hasIn = existingPunches.Any(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase));
        bool hasOut = existingPunches.Any(p => string.Equals(p.PunchType, "OUT", StringComparison.OrdinalIgnoreCase));

        // Rule: Reject duplicate IN (One IN per employee per day)
        if (normType == "IN" && hasIn)
        {
            var existingIn = existingPunches.First(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase));
            return (false, $"Duplicate Punch IN: {employee.FullName} already punched IN today at {existingIn.PunchTime:hh:mm tt}.", null);
        }

        // Rule: Reject OUT without IN
        if (normType == "OUT" && !hasIn)
        {
            return (false, $"Punch OUT rejected: {employee.FullName} has not punched IN today.", null);
        }

        // Rule: Reject duplicate OUT (One OUT per employee per day)
        if (normType == "OUT" && hasOut)
        {
            var existingOut = existingPunches.First(p => string.Equals(p.PunchType, "OUT", StringComparison.OrdinalIgnoreCase));
            return (false, $"Duplicate Punch OUT: {employee.FullName} already punched OUT today at {existingOut.PunchTime:hh:mm tt}.", null);
        }

        // Save captured verification photo if provided
        string? savedPhotoPath = null;
        if (!string.IsNullOrWhiteSpace(capturedPhotoBase64))
        {
            savedPhotoPath = await SaveCapturedPunchPhotoAsync(capturedPhotoBase64, employee.EmployeeNo, normType);
        }

        // Fallback to employee's profile photo if camera capture was not available
        if (string.IsNullOrEmpty(savedPhotoPath) && !string.IsNullOrEmpty(employee.Photo))
        {
            savedPhotoPath = employee.Photo;
        }

        // Check if Late
        bool isLate = false;
        if (normType == "IN")
        {
            if (TimeOnly.TryParse(_options.ShiftStartTime, out var shiftStart))
            {
                var punchTimeOnly = TimeOnly.FromDateTime(localPunchTime);
                if (punchTimeOnly > shiftStart)
                {
                    isLate = true;
                }
            }
        }

        var newPunch = new AttendanceModel
        {
            AttendanceId = Guid.NewGuid().ToString(),
            EmployeeId = employee.EmployeeId,
            EmployeeNo = employee.EmployeeNo,
            EmployeeName = employee.FullName,
            DepartmentName = employee.DepartmentName,
            AttendanceDate = today,
            PunchType = normType,
            PunchTime = localPunchTime,
            CapturedPhotoPath = savedPhotoPath,
            MatchScore = matchScore,
            VerificationMethod = verificationMethod,
            IsLate = isLate,
            Remarks = isLate ? "Late Arrival" : "On-Time",
            CreatedDate = serverNowUtc
        };

        await _attendanceRepo.AddAsync(newPunch);

        var successMessage = normType == "IN"
            ? (isLate
                ? $"Punch IN recorded successfully at {localPunchTime:hh:mm tt} (Late Arrival)."
                : $"Punch IN recorded successfully at {localPunchTime:hh:mm tt}.")
            : $"Punch OUT recorded successfully at {localPunchTime:hh:mm tt}. Have a great evening!";

        _logger.LogInformation("Recorded {PunchType} for {EmployeeNo} at {PunchTime}. Method: {Method}",
            normType, employee.EmployeeNo, localPunchTime, verificationMethod);

        return (true, successMessage, newPunch);
    }

    public async Task<DailyAttendanceViewModel> GetDailyAttendanceAsync(DateOnly date, string? departmentId = null, string? searchTerm = null)
    {
        var allEmployees = await _employeeRepo.GetAllAsync();
        var departments = await _departmentRepo.GetAllAsync();
        var deptDict = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        var query = allEmployees.Where(e => e.IsActive).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            query = query.Where(e => string.Equals(e.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(e =>
                e.EmployeeNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.FullName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var empList = query.ToList();
        var attendanceForDate = await _attendanceRepo.GetByDateAsync(date);
        var punchesByEmp = attendanceForDate
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.PunchTime).ToList());

        if (!TimeOnly.TryParse(_options.ShiftStartTime, out var shiftStart))
        {
            shiftStart = new TimeOnly(9, 0, 0);
        }

        var rows = new List<DailyAttendanceRow>();
        int presentCount = 0;
        int insideCount = 0;
        int lateCount = 0;

        foreach (var emp in empList)
        {
            var hasPunches = punchesByEmp.TryGetValue(emp.EmployeeId, out var empPunches);
            var firstIn = empPunches?.FirstOrDefault(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase));
            var lastOut = empPunches?.LastOrDefault(p => string.Equals(p.PunchType, "OUT", StringComparison.OrdinalIgnoreCase));

            bool isPresent = firstIn != null;
            bool isLate = false;
            string status = "Absent";
            string badgeClass = "bg-danger";
            string workingHours = "--:--";

            if (isPresent)
            {
                presentCount++;
                var punchTimeOnly = TimeOnly.FromDateTime(firstIn!.PunchTime);
                if (punchTimeOnly > shiftStart)
                {
                    isLate = true;
                    lateCount++;
                }

                var lastPunch = empPunches!.Last();
                if (string.Equals(lastPunch.PunchType, "IN", StringComparison.OrdinalIgnoreCase))
                {
                    insideCount++;
                    status = isLate ? "Late (Inside)" : "Currently Inside";
                    badgeClass = isLate ? "bg-warning text-dark" : "bg-primary";
                }
                else
                {
                    status = isLate ? "Late (Completed)" : "Completed";
                    badgeClass = isLate ? "bg-warning text-dark" : "bg-success";
                }

                if (lastOut != null && lastOut.PunchTime > firstIn.PunchTime)
                {
                    var span = lastOut.PunchTime - firstIn.PunchTime;
                    workingHours = $"{(int)span.TotalHours:D2}h {span.Minutes:D2}m";
                }
            }

            var deptName = !string.IsNullOrEmpty(emp.DepartmentId) && deptDict.TryGetValue(emp.DepartmentId, out var dn)
                ? dn
                : (emp.DepartmentName ?? "Unassigned");

            rows.Add(new DailyAttendanceRow
            {
                EmployeeId = emp.EmployeeId,
                EmployeeNo = emp.EmployeeNo,
                EmployeeName = emp.FullName,
                DepartmentName = deptName,
                PhotoPath = emp.Photo,
                FirstInTime = firstIn?.PunchTime,
                LastOutTime = lastOut?.PunchTime,
                WorkingHoursFormatted = workingHours,
                AttendanceStatus = status,
                StatusBadgeClass = badgeClass
            });
        }

        int total = empList.Count;
        int absent = Math.Max(0, total - presentCount);

        return new DailyAttendanceViewModel
        {
            Date = date,
            DepartmentId = departmentId,
            SearchTerm = searchTerm,
            Departments = departments.Where(d => d.IsActive).ToList(),
            Records = rows.OrderBy(r => r.AttendanceStatus == "Absent" ? 1 : 0).ThenBy(r => r.EmployeeNo).ToList(),
            TotalEmployees = total,
            PresentCount = presentCount,
            AbsentCount = absent,
            InsideCount = insideCount,
            LateCount = lateCount
        };
    }

    public async Task<AttendanceHistoryViewModel> GetEmployeeHistoryAsync(string? employeeId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var allEmployees = await _employeeRepo.GetAllAsync();
        var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        EmployeeModel? selectedEmp = null;
        if (!string.IsNullOrWhiteSpace(employeeId))
        {
            selectedEmp = allEmployees.FirstOrDefault(e => string.Equals(e.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase));
        }

        if (selectedEmp == null && allEmployees.Count > 0)
        {
            selectedEmp = allEmployees.First();
        }

        IReadOnlyList<AttendanceModel> records = new List<AttendanceModel>();
        if (selectedEmp != null)
        {
            records = await _attendanceRepo.GetByEmployeeIdAsync(selectedEmp.EmployeeId, start, end);
        }

        return new AttendanceHistoryViewModel
        {
            SelectedEmployeeId = selectedEmp?.EmployeeId,
            SelectedEmployeeNo = selectedEmp?.EmployeeNo,
            SelectedEmployeeName = selectedEmp?.FullName,
            StartDate = start,
            EndDate = end,
            Employees = allEmployees.Where(e => e.IsActive).OrderBy(e => e.EmployeeNo).ToList(),
            Records = records
        };
    }

    public async Task<AttendanceTerminalViewModel> GetTerminalViewModelAsync()
    {
        var allEmployees = await _employeeRepo.GetAllAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var todayPunches = await _attendanceRepo.GetByDateAsync(today);

        var recentActivities = todayPunches
            .OrderByDescending(p => p.PunchTime)
            .Take(6)
            .Select(p => new AttendanceActivityItem
            {
                AttendanceId = p.AttendanceId,
                EmployeeNo = p.EmployeeNo ?? "EMP",
                EmployeeName = p.EmployeeName ?? "Employee",
                DepartmentName = p.DepartmentName,
                PhotoPath = p.CapturedPhotoPath,
                PunchType = p.PunchType,
                PunchTime = p.PunchTime,
                VerificationMethod = p.VerificationMethod,
                MatchScore = p.MatchScore,
                IsLate = p.IsLate
            })
            .ToList();

        return new AttendanceTerminalViewModel
        {
            CompanyName = _options.CompanyName,
            ShiftStartTime = _options.ShiftStartTime,
            FaceMatchThreshold = _options.FaceMatchThreshold,
            TodayPunchesCount = todayPunches.Count,
            RecentTerminalActivity = recentActivities,
            RegisteredEmployees = allEmployees.Where(e => e.IsActive).OrderBy(e => e.EmployeeNo).ToList()
        };
    }

    private async Task<string?> SaveCapturedPunchPhotoAsync(string base64Image, string employeeNo, string punchType)
    {
        try
        {
            var cleanBase64 = base64Image;
            if (cleanBase64.Contains(','))
            {
                cleanBase64 = cleanBase64.Substring(cleanBase64.IndexOf(',') + 1);
            }

            var bytes = Convert.FromBase64String(cleanBase64);
            var sanitizedNo = string.Concat(employeeNo.Where(char.IsLetterOrDigit)).ToLowerInvariant();
            var fileName = $"punch_{sanitizedNo}_{punchType.ToLowerInvariant()}_{Guid.NewGuid():N}.jpg";
            var filePath = Path.Combine(_pathProvider.AttendanceUploadsDirectory, fileName);

            await File.WriteAllBytesAsync(filePath, bytes);
            return $"/uploads/attendance/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save punch verification photo for {EmployeeNo}", employeeNo);
            return null;
        }
    }
}
