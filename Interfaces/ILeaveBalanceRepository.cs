using HRMS.Models;

namespace HRMS.Interfaces;

public interface ILeaveBalanceRepository
{
    Task<IReadOnlyList<EmployeeLeaveBalanceModel>> GetAllAsync();
    Task<IReadOnlyList<EmployeeLeaveBalanceModel>> GetByEmployeeIdAsync(string employeeId, int financialYear);
    Task<EmployeeLeaveBalanceModel?> GetByEmployeeAndLeaveTypeAsync(string employeeId, string leaveTypeId, int financialYear);
    Task AddAsync(EmployeeLeaveBalanceModel balance);
    Task UpdateAsync(EmployeeLeaveBalanceModel balance);
    Task DeleteAsync(string leaveBalanceId);
}

