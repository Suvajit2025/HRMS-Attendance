using HRMS.Models;

namespace HRMS.Interfaces;

public interface ILeaveApplicationRepository
{
    Task<IReadOnlyList<LeaveApplicationModel>> GetAllAsync();
    Task<LeaveApplicationModel?> GetByIdAsync(string applicationId);
    Task<IReadOnlyList<LeaveApplicationModel>> GetByEmployeeIdAsync(string employeeId);
    Task<IReadOnlyList<LeaveApplicationModel>> GetPendingApplicationsAsync();
    Task AddAsync(LeaveApplicationModel application);
    Task UpdateAsync(LeaveApplicationModel application);
    Task DeleteAsync(string applicationId);
}

