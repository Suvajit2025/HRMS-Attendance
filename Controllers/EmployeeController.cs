using HRMS.Interfaces;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IFaceRecognitionService _faceService;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        IEmployeeService employeeService,
        IFaceRecognitionService faceService,
        IAttendanceRepository attendanceRepo,
        ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _faceService = faceService;
        _attendanceRepo = attendanceRepo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? departmentId, string? statusFilter, int page = 1)
    {
        var model = await _employeeService.GetPagedEmployeesAsync(searchTerm, departmentId, statusFilter, page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null) return NotFound();

        var hasFace = await _faceService.HasRegisteredFaceAsync(id);
        var recentAttendance = await _attendanceRepo.GetByEmployeeIdAsync(id, DateOnly.FromDateTime(DateTime.Today.AddDays(-15)));

        var viewModel = new EmployeeDetailsViewModel
        {
            Employee = employee,
            DepartmentName = employee.DepartmentName,
            HasFaceRegistered = hasFace,
            RecentAttendance = recentAttendance
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var departments = await _employeeService.GetActiveDepartmentsAsync();
        var nextNo = await _employeeService.GetNextEmployeeNoAsync();
        var model = new EmployeeEditViewModel
        {
            EmployeeNo = nextNo,
            Departments = departments,
            JoiningDate = DateOnly.FromDateTime(DateTime.Today),
            DOB = new DateOnly(1995, 1, 1),
            IsActive = true
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Departments = await _employeeService.GetActiveDepartmentsAsync();
            return View(model);
        }

        var (success, errorMessage) = await _employeeService.CreateEmployeeAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to create employee.");
            model.Departments = await _employeeService.GetActiveDepartmentsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Employee '{model.EmployeeNo} - {model.FirstName} {model.LastName}' was successfully added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null) return NotFound();

        var departments = await _employeeService.GetActiveDepartmentsAsync();
        var model = new EmployeeEditViewModel
        {
            EmployeeId = employee.EmployeeId,
            EmployeeNo = employee.EmployeeNo,
            FirstName = employee.FirstName,
            MiddleName = employee.MiddleName,
            LastName = employee.LastName,
            DOB = employee.DOB,
            JoiningDate = employee.JoiningDate,
            AadhaarNo = employee.AadhaarNo,
            PhoneNo = employee.PhoneNo,
            Gender = employee.Gender,
            DepartmentId = employee.DepartmentId,
            IsActive = employee.IsActive,
            ExistingPhotoPath = employee.Photo,
            Departments = departments
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Departments = await _employeeService.GetActiveDepartmentsAsync();
            return View(model);
        }

        var (success, errorMessage) = await _employeeService.UpdateEmployeeAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update employee.");
            model.Departments = await _employeeService.GetActiveDepartmentsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Employee '{model.EmployeeNo}' was successfully updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Invalid ID" });

        var success = await _employeeService.ToggleEmployeeStatusAsync(id);
        if (success)
        {
            return Json(new { success = true });
        }

        return Json(new { success = false, message = "Employee not found." });
    }
}

