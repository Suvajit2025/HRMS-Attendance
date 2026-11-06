using HRMS.Models;

namespace HRMS.Interfaces;

public interface ILeaveTransactionRepository
{
    Task<IReadOnlyList<LeaveTransactionModel>> GetAllAsync();
    Task<IReadOnlyList<LeaveTransactionModel>> GetByEmployeeIdAsync(string employeeId);
    Task<IReadOnlyList<LeaveTransactionModel>> GetByApplicationIdAsync(string applicationId);
    Task AddAsync(LeaveTransactionModel transaction);
}

