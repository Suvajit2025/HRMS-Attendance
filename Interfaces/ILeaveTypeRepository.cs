using HRMS.Models;

namespace HRMS.Interfaces;

public interface ILeaveTypeRepository
{
    Task<IReadOnlyList<LeaveTypeModel>> GetAllAsync();
    Task<LeaveTypeModel?> GetByIdAsync(string leaveTypeId);
    Task<LeaveTypeModel?> GetByCodeAsync(string leaveCode);
    Task AddAsync(LeaveTypeModel leaveType);
    Task UpdateAsync(LeaveTypeModel leaveType);
    Task DeleteAsync(string leaveTypeId);
}

