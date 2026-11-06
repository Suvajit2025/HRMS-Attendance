using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class LeaveApplicationController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly ILeaveCalculationService _calcService;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<LeaveApplicationController> _logger;

    public LeaveApplicationController(
        ILeaveService leaveService,
        IEmployeeRepository empRepo,
        ILeaveTypeRepository leaveTypeRepo,
        IDepartmentRepository deptRepo,
        ILeaveCalculationService calcService,
        StoragePathProvider pathProvider,
        ILogger<LeaveApplicationController> logger)
    {
        _leaveService = leaveService;
        _empRepo = empRepo;
        _leaveTypeRepo = leaveTypeRepo;
        _deptRepo = deptRepo;
        _calcService = calcService;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    // GET: /LeaveApplication
    public async Task<IActionResult> Index(string? employeeId, string? status, string? leaveTypeId)
    {
        var applications = await _leaveService.GetAllApplicationsAsync(employeeId, status, leaveTypeId);
        var employees = await _empRepo.GetAllAsync();
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();
        var departments = await _deptRepo.GetAllAsync();

        ViewBag.Employees = employees.OrderBy(e => e.FullName).ToList();
        ViewBag.LeaveTypes = leaveTypes.Where(t => t.IsActive).OrderBy(t => t.LeaveName).ToList();
        ViewBag.EmployeeLookup = employees.ToDictionary(e => e.EmployeeId, e => e, StringComparer.OrdinalIgnoreCase);
        ViewBag.LeaveTypeLookup = leaveTypes.ToDictionary(t => t.LeaveTypeId, t => t, StringComparer.OrdinalIgnoreCase);
        ViewBag.DepartmentLookup = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        ViewBag.SelectedEmployeeId = employeeId;
        ViewBag.SelectedStatus = status ?? "All";
        ViewBag.SelectedLeaveTypeId = leaveTypeId ?? "All";

        return View(applications);
    }

    // GET: /LeaveApplication/Create
    public async Task<IActionResult> Create(string? employeeId)
    {
        var employees = await _empRepo.GetAllAsync();
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();

        var model = new LeaveApplicationCreateViewModel
        {
            EmployeeId = employeeId ?? employees.FirstOrDefault(e => e.IsActive)?.EmployeeId ?? "",
            FromDate = DateOnly.FromDateTime(DateTime.Today),
            ToDate = DateOnly.FromDateTime(DateTime.Today),
            Employees = employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToList(),
            LeaveTypes = leaveTypes.Where(t => t.IsActive).OrderBy(t => t.LeaveName).ToList()
        };

        return View(model);
    }

    // POST: /LeaveApplication/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveApplicationCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        string? documentPath = null;
        if (model.SupportingDocument != null && model.SupportingDocument.Length > 0)
        {
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(model.SupportingDocument.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError(nameof(model.SupportingDocument), "Only PDF, JPG, or PNG files are permitted.");
                await PopulateDropdowns(model);
                return View(model);
            }

            if (model.SupportingDocument.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.SupportingDocument), "Document size must not exceed 5 MB.");
                await PopulateDropdowns(model);
                return View(model);
            }

            var fileName = $"LEAVE_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(_pathProvider.LeaveDocumentsUploadsDirectory, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await model.SupportingDocument.CopyToAsync(stream);
            }
            documentPath = $"/uploads/LeaveDocuments/{fileName}";
        }

        var application = new LeaveApplicationModel
        {
            EmployeeId = model.EmployeeId,
            LeaveTypeId = model.LeaveTypeId,
            FromDate = model.FromDate,
            ToDate = model.IsHalfDay ? model.FromDate : model.ToDate,
            IsHalfDay = model.IsHalfDay,
            HalfDaySession = model.IsHalfDay ? model.HalfDaySession : null,
            Reason = model.Reason,
            SupportingDocumentPath = documentPath
        };

        var (success, error, created) = await _leaveService.SubmitLeaveApplicationAsync(application);
        if (!success)
        {
            ModelState.AddModelError("", error ?? "Failed to submit leave application.");
            await PopulateDropdowns(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Leave application submitted successfully for {created?.TotalDays} day(s). Awaiting Admin approval.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /LeaveApplication/Details/{id}
    public async Task<IActionResult> Details(string id)
    {
        var application = await _leaveService.GetApplicationByIdAsync(id);
        if (application == null) return NotFound();

        var employee = await _empRepo.GetByIdAsync(application.EmployeeId);
        var leaveType = await _leaveTypeRepo.GetByIdAsync(application.LeaveTypeId)
                        ?? await _leaveTypeRepo.GetByCodeAsync(application.LeaveTypeId);
        var departments = await _deptRepo.GetAllAsync();
        var deptName = employee?.DepartmentId != null
            ? departments.FirstOrDefault(d => string.Equals(d.DepartmentId, employee.DepartmentId, StringComparison.OrdinalIgnoreCase))?.DepartmentName
            : "General";

        ViewBag.Employee = employee;
        ViewBag.LeaveType = leaveType;
        ViewBag.DepartmentName = deptName;

        return View(application);
    }

    // POST: /LeaveApplication/Cancel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string id, string cancellationReason)
    {
        var (success, error) = await _leaveService.CancelLeaveApplicationAsync(id, cancellationReason, User.Identity?.Name ?? "Admin");
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to cancel leave application.";
        }
        else
        {
            TempData["SuccessMessage"] = "Leave application has been cancelled.";
        }
        return RedirectToAction(nameof(Index));
    }

    // AJAX Endpoint: /LeaveApplication/CalculateDuration
    [HttpGet]
    public async Task<IActionResult> CalculateDuration(DateOnly fromDate, DateOnly toDate, string leaveTypeId, string? employeeId, bool isHalfDay = false, string? halfDaySession = null)
    {
        string? deptId = null;
        if (!string.IsNullOrEmpty(employeeId))
        {
            var emp = await _empRepo.GetByIdAsync(employeeId);
            deptId = emp?.DepartmentId;
        }

        var result = await _calcService.CalculateDurationAsync(fromDate, toDate, leaveTypeId, deptId, null, isHalfDay, halfDaySession);
        return Json(new
        {
            totalDays = result.TotalDays,
            calendarDays = result.CalendarDays,
            weeklyOffDays = result.WeeklyOffDays,
            holidayDays = result.HolidayDays,
            overlapDays = result.OverlapDays,
            breakdownSummary = result.BreakdownSummary,
            isSandwichApplied = result.IsSandwichApplied
        });
    }

    private async Task PopulateDropdowns(LeaveApplicationCreateViewModel model)
    {
        var employees = await _empRepo.GetAllAsync();
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();
        model.Employees = employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToList();
        model.LeaveTypes = leaveTypes.Where(t => t.IsActive).OrderBy(t => t.LeaveName).ToList();
    }
}

