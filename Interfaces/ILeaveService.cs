using HRMS.Models;
using HRMS.ViewModels;

namespace HRMS.Interfaces;

public interface ILeaveService
{
    Task<IReadOnlyList<LeaveApplicationModel>> GetAllApplicationsAsync(string? employeeId = null, string? status = null, string? leaveTypeId = null);
    Task<LeaveApplicationModel?> GetApplicationByIdAsync(string applicationId);
    Task<(bool Success, string? ErrorMessage, LeaveApplicationModel? Application)> SubmitLeaveApplicationAsync(LeaveApplicationModel application);
    Task<(bool Success, string? ErrorMessage)> ApproveLeaveApplicationAsync(string applicationId, string approvedBy);
    Task<(bool Success, string? ErrorMessage)> RejectLeaveApplicationAsync(string applicationId, string rejectionReason, string rejectedBy);
    Task<(bool Success, string? ErrorMessage)> CancelLeaveApplicationAsync(string applicationId, string cancellationReason, string cancelledBy);
    Task<IReadOnlyList<EmployeeLeaveBalanceModel>> GetEmployeeBalancesAsync(string employeeId, int? financialYear = null);
    Task<EmployeeLeaveBalanceModel?> GetEmployeeBalanceAsync(string employeeId, string leaveTypeId, int? financialYear = null);
    Task<(bool Success, string? ErrorMessage)> AdjustLeaveBalanceAsync(string employeeId, string leaveTypeId, double adjustmentAmount, string reason, string adjustedBy);
    Task<IReadOnlyList<LeaveTransactionModel>> GetTransactionsByEmployeeAsync(string employeeId);
    Task<IReadOnlyList<LeaveTransactionModel>> GetAllTransactionsAsync();
    Task EnsureEmployeeBalancesInitializedAsync(string employeeId, int financialYear);
    Task<IReadOnlyList<LeaveApplicationModel>> GetApprovedLeavesForEmployeeAsync(string employeeId, DateOnly startDate, DateOnly endDate);
}

