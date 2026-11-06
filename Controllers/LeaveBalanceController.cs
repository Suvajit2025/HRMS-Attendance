using HRMS.Interfaces;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class LeaveBalanceController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly IDepartmentRepository _deptRepo;

    public LeaveBalanceController(
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

    // GET: /LeaveBalance
    public async Task<IActionResult> Index(int? year, string? departmentId, string? searchTerm)
    {
        int finYear = year ?? DateTime.Today.Year;
        var employees = await _empRepo.GetAllAsync();
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();
        var departments = await _deptRepo.GetAllAsync();
        var deptDict = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        var query = employees.AsEnumerable();
        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(e => string.Equals(e.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(e => e.EmployeeNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                     e.FullName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var list = new List<EmployeeLeaveBalanceSummaryViewModel>();
        foreach (var emp in query.OrderBy(e => e.EmployeeNo))
        {
            var balances = await _leaveService.GetEmployeeBalancesAsync(emp.EmployeeId, finYear);
            var balanceLookup = balances.ToDictionary(b => b.LeaveTypeId, b => b, StringComparer.OrdinalIgnoreCase);

            var items = new List<EmployeeLeaveBalanceItemViewModel>();
            foreach (var lt in leaveTypes.Where(t => t.IsActive))
            {
                balanceLookup.TryGetValue(lt.LeaveTypeId, out var b);
                items.Add(new EmployeeLeaveBalanceItemViewModel
                {
                    LeaveTypeId = lt.LeaveTypeId,
                    LeaveCode = lt.LeaveCode,
                    LeaveName = lt.LeaveName,
                    IsPaidLeave = lt.IsPaidLeave,
                    OpeningBalance = b?.OpeningBalance ?? 0,
                    Accrued = b?.Accrued ?? 0,
                    Used = b?.Used ?? 0,
                    Reserved = b?.Reserved ?? 0,
                    Adjusted = b?.Adjusted ?? 0,
                    AvailableBalance = b?.AvailableBalance ?? 0
                });
            }

            var deptName = !string.IsNullOrEmpty(emp.DepartmentId) && deptDict.TryGetValue(emp.DepartmentId, out var dn)
                ? dn
                : (emp.DepartmentName ?? "General");

            list.Add(new EmployeeLeaveBalanceSummaryViewModel
            {
                Employee = emp,
                DepartmentName = deptName,
                FinancialYear = finYear,
                Balances = items
            });
        }

        ViewBag.SelectedYear = finYear;
        ViewBag.SelectedDepartmentId = departmentId;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.Departments = departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList();
        ViewBag.LeaveTypes = leaveTypes.Where(t => t.IsActive).OrderBy(t => t.LeaveCode).ToList();

        return View(list);
    }

    // GET: /LeaveBalance/Details/{employeeId}
    public async Task<IActionResult> Details(string employeeId, int? year)
    {
        int finYear = year ?? DateTime.Today.Year;
        var employee = await _empRepo.GetByIdAsync(employeeId);
        if (employee == null) return NotFound();

        var departments = await _deptRepo.GetAllAsync();
        var deptName = employee.DepartmentId != null
            ? departments.FirstOrDefault(d => string.Equals(d.DepartmentId, employee.DepartmentId, StringComparison.OrdinalIgnoreCase))?.DepartmentName
            : "General";

        var balances = await _leaveService.GetEmployeeBalancesAsync(employeeId, finYear);
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();
        var typeDict = leaveTypes.ToDictionary(t => t.LeaveTypeId, t => t, StringComparer.OrdinalIgnoreCase);

        var transactions = await _leaveService.GetTransactionsByEmployeeAsync(employeeId);

        ViewBag.Employee = employee;
        ViewBag.DepartmentName = deptName;
        ViewBag.FinancialYear = finYear;
        ViewBag.LeaveTypes = typeDict;
        ViewBag.Transactions = transactions;

        return View(balances);
    }

    // POST: /LeaveBalance/Adjust
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(string employeeId, string leaveTypeId, double amount, string reason, int year)
    {
        if (amount == 0 || string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "A valid adjustment amount and reason are required.";
            return RedirectToAction(nameof(Details), new { employeeId, year });
        }

        var (success, error) = await _leaveService.AdjustLeaveBalanceAsync(employeeId, leaveTypeId, amount, reason, User.Identity?.Name ?? "Admin");
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to adjust leave balance.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Leave balance adjusted by {amount:+#;-#;0} day(s). Ledger updated.";
        }

        return RedirectToAction(nameof(Details), new { employeeId, year });
    }
}

