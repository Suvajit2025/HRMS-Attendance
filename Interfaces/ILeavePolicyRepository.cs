using HRMS.Models;

namespace HRMS.Interfaces;

public interface ILeavePolicyRepository
{
    Task<IReadOnlyList<LeavePolicyModel>> GetAllAsync();
    Task<LeavePolicyModel?> GetByIdAsync(string leavePolicyId);
    Task<IReadOnlyList<LeavePolicyModel>> GetByLeaveTypeIdAsync(string leaveTypeId);
    Task AddAsync(LeavePolicyModel policy);
    Task UpdateAsync(LeavePolicyModel policy);
    Task DeleteAsync(string leavePolicyId);
}

