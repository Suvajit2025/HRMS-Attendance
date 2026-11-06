using HRMS.Interfaces;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class AttendanceReportController : Controller
{
    private readonly IAttendanceReportService _reportService;
    private readonly IAttendanceExcelExportService _excelExportService;
    private readonly ILogger<AttendanceReportController> _logger;

    public AttendanceReportController(
        IAttendanceReportService reportService,
        IAttendanceExcelExportService excelExportService,
        ILogger<AttendanceReportController> logger)
    {
        _reportService = reportService;
        _excelExportService = excelExportService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AttendanceReportFilterModel filter)
    {
        string preparedBy = User.Identity?.Name ?? "HR Administrator";
        var model = await _reportService.GenerateMonthlyReportAsync(filter, preparedBy);

        // If AJAX request for table only, return partial view
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_AttendanceReportTable", model);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadExcel([FromQuery] AttendanceReportFilterModel filter)
    {
        string preparedBy = User.Identity?.Name ?? "HR Administrator";
        var model = await _reportService.GenerateMonthlyReportAsync(filter, preparedBy);

        var excelBytes = _excelExportService.ExportMonthlyReportToExcel(model);
        string fileName = $"HRMS_Attendance_Report_{model.Filter.Year}_{model.Filter.Month:D2}.xlsx";

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}

