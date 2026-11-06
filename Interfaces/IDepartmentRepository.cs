using HRMS.Models;

namespace HRMS.Interfaces;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<DepartmentModel>> GetAllAsync();
    Task<DepartmentModel?> GetByIdAsync(string departmentId);
    Task AddAsync(DepartmentModel department);
    Task UpdateAsync(DepartmentModel department);
    Task DeleteAsync(string departmentId);
}

