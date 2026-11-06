using System.Text;
using HRMS.Interfaces;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

[Authorize]
public class LeaveReportController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly IDepartmentRepository _deptRepo;

    public LeaveReportController(
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

    // GET: /LeaveReport/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var employees = await _empRepo.GetAllAsync();
        var activeEmps = employees.Where(e => e.IsActive).ToList();
        var applications = await _leaveService.GetAllApplicationsAsync();
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();
        var departments = await _deptRepo.GetAllAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var pendingApps = applications.Where(a => a.Status == "Pending").ToList();
        var approvedApps = applications.Where(a => a.Status == "Approved").ToList();
        var todayLeaves = approvedApps.Where(a => a.FromDate <= today && a.ToDate >= today).ToList();

        // Calculate total available balance across all active employees
        double totalBalance = 0;
        foreach (var emp in activeEmps)
        {
            var balances = await _leaveService.GetEmployeeBalancesAsync(emp.EmployeeId, today.Year);
            totalBalance += balances.Sum(b => b.AvailableBalance);
        }

        var vm = new LeaveDashboardViewModel
        {
            TotalEmployees = activeEmps.Count,
            PendingApplicationsCount = pendingApps.Count,
            ApprovedApplicationsCount = approvedApps.Count,
            EmployeesOnLeaveTodayCount = todayLeaves.Count,
            TotalAvailableBalance = Math.Round(totalBalance, 1),
            PendingApplications = pendingApps.Take(10).ToList(),
            TodayLeaves = todayLeaves,
            EmployeeLookup = employees.ToDictionary(e => e.EmployeeId, e => e, StringComparer.OrdinalIgnoreCase),
            LeaveTypeLookup = leaveTypes.ToDictionary(t => t.LeaveTypeId, t => t, StringComparer.OrdinalIgnoreCase)
        };

        return View(vm);
    }

    // GET: /LeaveReport
    public async Task<IActionResult> Index(LeaveReportFilterModel filter)
    {
        var applications = await _leaveService.GetAllApplicationsAsync(status: filter.Status, leaveTypeId: filter.LeaveTypeId);
        var employees = await _empRepo.GetAllAsync();
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();
        var departments = await _deptRepo.GetAllAsync();

        var empDict = employees.ToDictionary(e => e.EmployeeId, e => e, StringComparer.OrdinalIgnoreCase);
        var typeDict = leaveTypes.ToDictionary(t => t.LeaveTypeId, t => t, StringComparer.OrdinalIgnoreCase);
        var deptDict = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        var query = applications.Where(a => a.FromDate <= filter.EndDate && a.ToDate >= filter.StartDate);

        var reportItems = new List<LeaveReportItemViewModel>();
        foreach (var app in query)
        {
            empDict.TryGetValue(app.EmployeeId, out var emp);
            if (emp == null) continue;

            if (!string.IsNullOrEmpty(filter.DepartmentId) && !string.Equals(emp.DepartmentId, filter.DepartmentId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                bool matches = emp.EmployeeNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                               emp.FullName.Contains(term, StringComparison.OrdinalIgnoreCase);
                if (!matches) continue;
            }

            typeDict.TryGetValue(app.LeaveTypeId, out var lt);
            var deptName = !string.IsNullOrEmpty(emp.DepartmentId) && deptDict.TryGetValue(emp.DepartmentId, out var dn) ? dn : "General";

            reportItems.Add(new LeaveReportItemViewModel
            {
                Application = app,
                EmployeeNo = emp.EmployeeNo,
                EmployeeName = emp.FullName,
                DepartmentName = deptName,
                LeaveCode = lt?.LeaveCode ?? app.LeaveTypeId,
                LeaveName = lt?.LeaveName ?? "Leave",
                IsPaid = lt?.IsPaidLeave ?? true
            });
        }

        var vm = new LeaveReportViewModel
        {
            Filter = filter,
            Items = reportItems.OrderByDescending(i => i.Application.FromDate).ToList(),
            Departments = departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList(),
            LeaveTypes = leaveTypes.Where(t => t.IsActive).OrderBy(t => t.LeaveName).ToList()
        };

        return View(vm);
    }

    // GET: /LeaveReport/DownloadExcel
    public async Task<IActionResult> DownloadExcel(LeaveReportFilterModel filter)
    {
        var applications = await _leaveService.GetAllApplicationsAsync(status: filter.Status, leaveTypeId: filter.LeaveTypeId);
        var employees = await _empRepo.GetAllAsync();
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();
        var departments = await _deptRepo.GetAllAsync();

        var empDict = employees.ToDictionary(e => e.EmployeeId, e => e, StringComparer.OrdinalIgnoreCase);
        var typeDict = leaveTypes.ToDictionary(t => t.LeaveTypeId, t => t, StringComparer.OrdinalIgnoreCase);
        var deptDict = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        var query = applications.Where(a => a.FromDate <= filter.EndDate && a.ToDate >= filter.StartDate);

        var sb = new StringBuilder();
        sb.AppendLine("Application ID,Employee No,Employee Name,Department,Leave Type,Paid/Unpaid,From Date,To Date,Days,Status,Reason,Approved By,Approved Date");

        foreach (var app in query.OrderByDescending(a => a.FromDate))
        {
            empDict.TryGetValue(app.EmployeeId, out var emp);
            if (emp == null) continue;

            if (!string.IsNullOrEmpty(filter.DepartmentId) && !string.Equals(emp.DepartmentId, filter.DepartmentId, StringComparison.OrdinalIgnoreCase))
                continue;

            typeDict.TryGetValue(app.LeaveTypeId, out var lt);
            var deptName = !string.IsNullOrEmpty(emp.DepartmentId) && deptDict.TryGetValue(emp.DepartmentId, out var dn) ? dn : "General";

            sb.AppendLine($"\"{app.LeaveApplicationId}\",\"{emp.EmployeeNo}\",\"{emp.FullName}\",\"{deptName}\",\"{lt?.LeaveName ?? app.LeaveTypeId}\",\"{(lt?.IsPaidLeave == true ? "Paid" : "Unpaid")}\",\"{app.FromDate:yyyy-MM-dd}\",\"{app.ToDate:yyyy-MM-dd}\",{app.TotalDays},\"{app.Status}\",\"{app.Reason?.Replace("\"", "\"\"")}\",\"{app.ApprovedBy}\",\"{app.ApprovedDate:yyyy-MM-dd}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"Leave_Report_{filter.StartDate:yyyyMMdd}_{filter.EndDate:yyyyMMdd}.csv");
    }

    // GET: /LeaveReport/Transactions
    public async Task<IActionResult> Transactions(string? employeeId)
    {
        var transactions = string.IsNullOrEmpty(employeeId)
            ? await _leaveService.GetAllTransactionsAsync()
            : await _leaveService.GetTransactionsByEmployeeAsync(employeeId);

        var employees = await _empRepo.GetAllAsync();
        var leaveTypes = await _leaveTypeRepo.GetAllAsync();

        ViewBag.Employees = employees.OrderBy(e => e.FullName).ToList();
        ViewBag.EmployeeLookup = employees.ToDictionary(e => e.EmployeeId, e => e, StringComparer.OrdinalIgnoreCase);
        ViewBag.LeaveTypeLookup = leaveTypes.ToDictionary(t => t.LeaveTypeId, t => t, StringComparer.OrdinalIgnoreCase);
        ViewBag.SelectedEmployeeId = employeeId;

        return View(transactions);
    }
}

