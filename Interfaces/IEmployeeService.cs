using HRMS.Models;
using HRMS.ViewModels;

namespace HRMS.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeListViewModel> GetPagedEmployeesAsync(string? searchTerm, string? departmentId, string? statusFilter, int page, int pageSize);
    Task<EmployeeModel?> GetEmployeeByIdAsync(string employeeId);
    Task<EmployeeModel?> GetEmployeeByNoAsync(string employeeNo);
    Task<(bool Success, string? ErrorMessage)> CreateEmployeeAsync(EmployeeEditViewModel model);
    Task<(bool Success, string? ErrorMessage)> UpdateEmployeeAsync(EmployeeEditViewModel model);
    Task<bool> ToggleEmployeeStatusAsync(string employeeId);
    Task<IReadOnlyList<DepartmentModel>> GetActiveDepartmentsAsync();
    Task<string> GetNextEmployeeNoAsync();
    Task<string?> SaveEmployeePhotoAsync(IFormFile? file, string? base64Image, string employeeNo);
}

