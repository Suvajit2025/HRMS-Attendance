using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;

namespace HRMS.Repositories;

public class JsonDepartmentRepository : IDepartmentRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonDepartmentRepository> _logger;

    public JsonDepartmentRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonDepartmentRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DepartmentModel>> GetAllAsync()
    {
        var departments = await _storage.ReadListAsync<DepartmentModel>(_pathProvider.DepartmentsFilePath);
        if (departments.Count == 0)
        {
            departments = GetSeedDepartments();
            await _storage.WriteAsync(_pathProvider.DepartmentsFilePath, departments);
        }
        return departments.OrderBy(d => d.DepartmentName).ToList();
    }

    public async Task<DepartmentModel?> GetByIdAsync(string departmentId)
    {
        var departments = await GetAllAsync();
        return departments.FirstOrDefault(d => string.Equals(d.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(DepartmentModel department)
    {
        var departments = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(department.DepartmentId))
        {
            department.DepartmentId = Guid.NewGuid().ToString();
        }
        department.CreatedDate = DateTime.UtcNow;
        departments.Add(department);
        await _storage.WriteAsync(_pathProvider.DepartmentsFilePath, departments);
    }

    public async Task UpdateAsync(DepartmentModel department)
    {
        var departments = (await GetAllAsync()).ToList();
        var index = departments.FindIndex(d => string.Equals(d.DepartmentId, department.DepartmentId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            departments[index] = department;
            await _storage.WriteAsync(_pathProvider.DepartmentsFilePath, departments);
        }
    }

    public async Task DeleteAsync(string departmentId)
    {
        var departments = (await GetAllAsync()).ToList();
        var index = departments.FindIndex(d => string.Equals(d.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            departments.RemoveAt(index);
            await _storage.WriteAsync(_pathProvider.DepartmentsFilePath, departments);
        }
    }

    private static List<DepartmentModel> GetSeedDepartments()
    {
        return new List<DepartmentModel>
        {
            new() { DepartmentId = "dept-prod-001", DepartmentName = "Production", Description = "Shop floor & manufacturing operations", IsActive = true },
            new() { DepartmentId = "dept-admin-002", DepartmentName = "Administration", Description = "Plant management & general administrative affairs", IsActive = true },
            new() { DepartmentId = "dept-hr-003", DepartmentName = "Human Resources", Description = "Workforce management & personnel", IsActive = true },
            new() { DepartmentId = "dept-sec-004", DepartmentName = "Security", Description = "Gate & perimeter security surveillance", IsActive = true }
        };
    }
}

