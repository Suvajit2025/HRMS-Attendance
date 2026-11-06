using HRMS.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        IDepartmentRepository departmentRepo,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _departmentRepo = departmentRepo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? date = null, string? departmentId = null, string? searchTerm = null)
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

        var model = await _dashboardService.GetDashboardDataAsync(selectedDate, departmentId, searchTerm);
        ViewBag.Departments = await _departmentRepo.GetAllAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetLiveStats(string? date = null, string? departmentId = null)
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

        var stats = await _dashboardService.GetDashboardDataAsync(selectedDate, departmentId);

        return Json(new
        {
            success = true,
            totalEmployees = stats.TotalEmployees,
            presentToday = stats.PresentToday,
            absentToday = stats.AbsentToday,
            currentlyInside = stats.CurrentlyInside,
            lateArrivals = stats.LateArrivals,
            attendancePercentage = stats.AttendancePercentage,
            recentActivities = stats.RecentActivities.Select(a => new
            {
                a.EmployeeNo,
                a.EmployeeName,
                a.DepartmentName,
                a.PunchType,
                punchTimeFormatted = a.PunchTime.ToString("hh:mm:ss tt"),
                a.VerificationMethod,
                matchScore = a.MatchScore.HasValue ? (int)(a.MatchScore.Value * 100) : 0,
                a.IsLate,
                photoPath = a.PhotoPath ?? "/images/avatar-default.png"
            }),
            trends = stats.Last7DaysTrends.Select(t => new
            {
                t.DateFormatted,
                t.PresentCount,
                t.AbsentCount,
                t.LateCount
            }),
            departmentStats = stats.DepartmentStats.Select(d => new
            {
                d.DepartmentName,
                d.PresentCount,
                d.AbsentCount,
                d.TotalCount
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportTodayCsv(string? date = null, string? departmentId = null)
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

        var bytes = await _dashboardService.GenerateAttendanceCsvAsync(selectedDate, departmentId);
        var filename = $"HRMS_Attendance_{selectedDate:yyyyMMdd}.csv";

        return File(bytes, "text/csv", filename);
    }
}

