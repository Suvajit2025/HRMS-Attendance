using HRMS.Models;

namespace HRMS.Interfaces;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<EmployeeModel>> GetAllAsync();
    Task<EmployeeModel?> GetByIdAsync(string employeeId);
    Task<EmployeeModel?> GetByEmployeeNoAsync(string employeeNo);
    Task AddAsync(EmployeeModel employee);
    Task UpdateAsync(EmployeeModel employee);
    Task DeleteAsync(string employeeId);
    Task<bool> ExistsEmployeeNoAsync(string employeeNo, string? excludeEmployeeId = null);
}

