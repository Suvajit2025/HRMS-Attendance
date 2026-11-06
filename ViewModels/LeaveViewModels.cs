using System.ComponentModel.DataAnnotations;
using HRMS.Models;
using Microsoft.AspNetCore.Http;

namespace HRMS.ViewModels;

public class LeaveApplicationCreateViewModel
{
    [Required(ErrorMessage = "Please select an employee.")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a leave type.")]
    public string LeaveTypeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "End date is required.")]
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public bool IsHalfDay { get; set; } = false;
    public string? HalfDaySession { get; set; } = "FirstHalf";

    [Required(ErrorMessage = "Please provide a reason for leave.")]
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string Reason { get; set; } = string.Empty;

    public IFormFile? SupportingDocument { get; set; }
    public double CalculatedDays { get; set; } = 1.0;
    public string? BreakdownSummary { get; set; }

    // Dropdowns
    public IReadOnlyList<EmployeeModel> Employees { get; set; } = new List<EmployeeModel>();
    public IReadOnlyList<LeaveTypeModel> LeaveTypes { get; set; } = new List<LeaveTypeModel>();
}

public class LeaveApprovalViewModel
{
    public LeaveApplicationModel Application { get; set; } = new();
    public EmployeeModel Employee { get; set; } = new();
    public LeaveTypeModel LeaveType { get; set; } = new();
    public string? DepartmentName { get; set; }
    public double AvailableBalance { get; set; }
    public string? RejectionReason { get; set; }
}

public class EmployeeLeaveBalanceItemViewModel
{
    public string LeaveTypeId { get; set; } = string.Empty;
    public string LeaveCode { get; set; } = string.Empty;
    public string LeaveName { get; set; } = string.Empty;
    public bool IsPaidLeave { get; set; }
    public double OpeningBalance { get; set; }
    public double Accrued { get; set; }
    public double Used { get; set; }
    public double Reserved { get; set; }
    public double Adjusted { get; set; }
    public double AvailableBalance { get; set; }
}

public class EmployeeLeaveBalanceSummaryViewModel
{
    public EmployeeModel Employee { get; set; } = new();
    public string? DepartmentName { get; set; }
    public int FinancialYear { get; set; }
    public List<EmployeeLeaveBalanceItemViewModel> Balances { get; set; } = new();
}

public class LeaveDashboardViewModel
{
    public int TotalEmployees { get; set; }
    public int PendingApplicationsCount { get; set; }
    public int ApprovedApplicationsCount { get; set; }
    public int EmployeesOnLeaveTodayCount { get; set; }
    public double TotalAvailableBalance { get; set; }

    public IReadOnlyList<LeaveApplicationModel> PendingApplications { get; set; } = new List<LeaveApplicationModel>();
    public IReadOnlyList<LeaveApplicationModel> TodayLeaves { get; set; } = new List<LeaveApplicationModel>();
    public Dictionary<string, int> LeaveTypeCounts { get; set; } = new();
    public Dictionary<string, int> DepartmentLeaveCounts { get; set; } = new();
    public Dictionary<string, EmployeeModel> EmployeeLookup { get; set; } = new();
    public Dictionary<string, LeaveTypeModel> LeaveTypeLookup { get; set; } = new();
}

public class LeaveReportFilterModel
{
    public DateOnly StartDate { get; set; } = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateOnly EndDate { get; set; } = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
    public string? DepartmentId { get; set; }
    public string? LeaveTypeId { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
}

public class LeaveReportItemViewModel
{
    public LeaveApplicationModel Application { get; set; } = new();
    public string EmployeeNo { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string LeaveCode { get; set; } = string.Empty;
    public string LeaveName { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
}

public class LeaveReportViewModel
{
    public LeaveReportFilterModel Filter { get; set; } = new();
    public List<LeaveReportItemViewModel> Items { get; set; } = new();
    public IReadOnlyList<DepartmentModel> Departments { get; set; } = new List<DepartmentModel>();
    public IReadOnlyList<LeaveTypeModel> LeaveTypes { get; set; } = new List<LeaveTypeModel>();
    public int TotalApplications => Items.Count;
    public double TotalApprovedDays => Items.Where(i => i.Application.Status == "Approved").Sum(i => i.Application.TotalDays);
    public double TotalPaidDays => Items.Where(i => i.Application.Status == "Approved" && i.IsPaid).Sum(i => i.Application.TotalDays);
    public double TotalUnpaidDays => Items.Where(i => i.Application.Status == "Approved" && !i.IsPaid).Sum(i => i.Application.TotalDays);
}

