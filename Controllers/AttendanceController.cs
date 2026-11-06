using HRMS.Interfaces;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly IFaceRecognitionService _faceService;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(
        IAttendanceService attendanceService,
        IFaceRecognitionService faceService,
        IEmployeeRepository employeeRepo,
        IDepartmentRepository departmentRepo,
        ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _faceService = faceService;
        _employeeRepo = employeeRepo;
        _departmentRepo = departmentRepo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Terminal()
    {
        var model = await _attendanceService.GetTerminalViewModelAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Daily(string? date = null, string? departmentId = null, string? searchTerm = null)
    {
        DateOnly selectedDate;
        if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var parsedDate))
        {
            selectedDate = parsedDate;
        }
        else
        {
            selectedDate = DateOnly.FromDateTime(DateTime.Today);
        }

        var model = await _attendanceService.GetDailyAttendanceAsync(selectedDate, departmentId, searchTerm);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> History(string? employeeId = null, string? startDate = null, string? endDate = null)
    {
        DateOnly? start = null;
        if (!string.IsNullOrEmpty(startDate) && DateOnly.TryParse(startDate, out var parsedStart))
        {
            start = parsedStart;
        }

        DateOnly? end = null;
        if (!string.IsNullOrEmpty(endDate) && DateOnly.TryParse(endDate, out var parsedEnd))
        {
            end = parsedEnd;
        }

        var model = await _attendanceService.GetEmployeeHistoryAsync(employeeId, start, end);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPunch([FromBody] PunchRequestDto request)
    {
        if (string.IsNullOrEmpty(request.EmployeeId))
        {
            return Json(new PunchResultDto
            {
                Success = false,
                Message = "Employee identification is required."
            });
        }

        var (success, message, punch) = await _attendanceService.ProcessPunchAsync(
            request.EmployeeId,
            request.PunchType,
            request.VerificationMethod ?? "Manual",
            null,
            request.CapturedPhotoBase64);

        if (!success || punch == null)
        {
            return Json(new PunchResultDto
            {
                Success = false,
                Message = message
            });
        }

        return Json(new PunchResultDto
        {
            Success = true,
            Message = message,
            PunchType = punch.PunchType,
            PunchTimeFormatted = punch.PunchTime.ToString("hh:mm:ss tt"),
            EmployeeNo = punch.EmployeeNo,
            EmployeeName = punch.EmployeeName,
            DepartmentName = punch.DepartmentName,
            PhotoPath = punch.CapturedPhotoPath,
            MatchScore = punch.MatchScore,
            IsLate = punch.IsLate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FacePunch([FromBody] FacePunchRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return Json(new PunchResultDto
            {
                Success = false,
                Message = "No camera frame received for face matching."
            });
        }

        var matchResult = await _faceService.MatchFaceAsync(request.ImageBase64);
        if (!matchResult.IsSuccess || string.IsNullOrEmpty(matchResult.EmployeeId))
        {
            return Json(new PunchResultDto
            {
                Success = false,
                Message = matchResult.Message
            });
        }

        var (success, message, punch) = await _attendanceService.ProcessPunchAsync(
            matchResult.EmployeeId,
            request.PunchType,
            "FaceRecognition",
            matchResult.Confidence,
            request.ImageBase64);

        if (!success || punch == null)
        {
            return Json(new PunchResultDto
            {
                Success = false,
                Message = message,
                EmployeeNo = matchResult.EmployeeNo,
                EmployeeName = matchResult.EmployeeName,
                MatchScore = matchResult.Confidence
            });
        }

        return Json(new PunchResultDto
        {
            Success = true,
            Message = message,
            PunchType = punch.PunchType,
            PunchTimeFormatted = punch.PunchTime.ToString("hh:mm:ss tt"),
            EmployeeNo = punch.EmployeeNo,
            EmployeeName = punch.EmployeeName,
            DepartmentName = punch.DepartmentName,
            PhotoPath = punch.CapturedPhotoPath,
            MatchScore = matchResult.Confidence,
            IsLate = punch.IsLate
        });
    }
}

