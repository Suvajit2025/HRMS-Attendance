using HRMS.Interfaces;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class LeaveApprovalController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly IDepartmentRepository _deptRepo;

    public LeaveApprovalController(
        ILeaveService leaveService,
        IEmployeeRepository empRepo,
        ILeaveTypeRepository leaveTypeRepo,
        IDepartmentRepository deptRepo)
    {
        _leaveService = leaveService;
        _empRepo = empRepo;
        _leaveTypeRepo = leaveTypeRepo;
        _deptRepo = deptRepo;
    }

    // GET: /LeaveApproval
    public async Task<IActionResult> Index()
    {
        var pending = await _leaveService.GetAllApplicationsAsync(status: "Pending");
        var employees = await _empRepo.GetAllAsync();
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();
        var departments = await _deptRepo.GetAllAsync();

        ViewBag.EmployeeLookup = employees.ToDictionary(e => e.EmployeeId, e => e, StringComparer.OrdinalIgnoreCase);
        ViewBag.LeaveTypeLookup = leaveTypes.ToDictionary(t => t.LeaveTypeId, t => t, StringComparer.OrdinalIgnoreCase);
        ViewBag.DepartmentLookup = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        return View(pending);
    }

    // GET: /LeaveApproval/Details/{id}
    public async Task<IActionResult> Details(string id)
    {
        var application = await _leaveService.GetApplicationByIdAsync(id);
        if (application == null) return NotFound();

        var employee = await _empRepo.GetByIdAsync(application.EmployeeId);
        if (employee == null) return NotFound();

        var leaveType = await _leaveTypeRepo.GetByIdAsync(application.LeaveTypeId)
                        ?? await _leaveTypeRepo.GetByCodeAsync(application.LeaveTypeId);
        var departments = await _deptRepo.GetAllAsync();
        var deptName = employee.DepartmentId != null
            ? departments.FirstOrDefault(d => string.Equals(d.DepartmentId, employee.DepartmentId, StringComparison.OrdinalIgnoreCase))?.DepartmentName
            : "General";

        var balance = await _leaveService.GetEmployeeBalanceAsync(employee.EmployeeId, application.LeaveTypeId, application.FromDate.Year);

        var vm = new LeaveApprovalViewModel
        {
            Application = application,
            Employee = employee,
            LeaveType = leaveType ?? new Models.LeaveTypeModel { LeaveName = "Leave" },
            DepartmentName = deptName,
            AvailableBalance = balance?.AvailableBalance ?? 0
        };

        return View(vm);
    }

    // POST: /LeaveApproval/Approve/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id)
    {
        var (success, error) = await _leaveService.ApproveLeaveApplicationAsync(id, User.Identity?.Name ?? "Admin");
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to approve leave application.";
        }
        else
        {
            TempData["SuccessMessage"] = "Leave application approved successfully. Attendance and balance ledger updated.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /LeaveApproval/Reject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string id, string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            TempData["ErrorMessage"] = "A rejection reason is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var (success, error) = await _leaveService.RejectLeaveApplicationAsync(id, rejectionReason, User.Identity?.Name ?? "Admin");
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to reject leave application.";
        }
        else
        {
            TempData["SuccessMessage"] = "Leave application rejected. Reserved balance released.";
        }
        return RedirectToAction(nameof(Index));
    }
}

