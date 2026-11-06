using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;

namespace HRMS.Repositories;

public class JsonEmployeeRepository : IEmployeeRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonEmployeeRepository> _logger;

    public JsonEmployeeRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonEmployeeRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmployeeModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.EmployeesFilePath))
        {
            var seed = GetSeedEmployees();
            await _storage.WriteAsync(_pathProvider.EmployeesFilePath, seed);
            return seed.OrderBy(e => e.EmployeeNo).ToList();
        }

        var employees = await _storage.ReadListAsync<EmployeeModel>(_pathProvider.EmployeesFilePath);
        return employees.OrderBy(e => e.EmployeeNo).ToList();
    }

    public async Task<EmployeeModel?> GetByIdAsync(string employeeId)
    {
        var employees = await GetAllAsync();
        return employees.FirstOrDefault(e => string.Equals(e.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<EmployeeModel?> GetByEmployeeNoAsync(string employeeNo)
    {
        var employees = await GetAllAsync();
        return employees.FirstOrDefault(e => string.Equals(e.EmployeeNo, employeeNo, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(EmployeeModel employee)
    {
        var employees = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(employee.EmployeeId))
        {
            employee.EmployeeId = Guid.NewGuid().ToString();
        }
        employee.CreatedDate = DateTime.UtcNow;
        employees.Add(employee);
        await _storage.WriteAsync(_pathProvider.EmployeesFilePath, employees);
    }

    public async Task UpdateAsync(EmployeeModel employee)
    {
        var employees = (await GetAllAsync()).ToList();
        var index = employees.FindIndex(e => string.Equals(e.EmployeeId, employee.EmployeeId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            employee.UpdatedDate = DateTime.UtcNow;
            employees[index] = employee;
            await _storage.WriteAsync(_pathProvider.EmployeesFilePath, employees);
        }
    }

    public async Task DeleteAsync(string employeeId)
    {
        var employees = (await GetAllAsync()).ToList();
        var index = employees.FindIndex(e => string.Equals(e.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            employees.RemoveAt(index);
            await _storage.WriteAsync(_pathProvider.EmployeesFilePath, employees);
        }
    }

    public async Task<bool> ExistsEmployeeNoAsync(string employeeNo, string? excludeEmployeeId = null)
    {
        var employees = await GetAllAsync();
        return employees.Any(e =>
            string.Equals(e.EmployeeNo, employeeNo, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrEmpty(excludeEmployeeId) || !string.Equals(e.EmployeeId, excludeEmployeeId, StringComparison.OrdinalIgnoreCase)));
    }

    private static List<EmployeeModel> GetSeedEmployees()
    {
        return new List<EmployeeModel>
        {
            new()
            {
                EmployeeId = "emp-001",
                EmployeeNo = "EMP001",
                FirstName = "Rajesh",
                LastName = "Sharma",
                DOB = new DateOnly(1988, 4, 15),
                JoiningDate = new DateOnly(2021, 3, 10),
                AadhaarNo = "982345091234",
                PhoneNo = "+91 98301 23456",
                Gender = "Male",
                DepartmentId = "dept-prod-001",
                DepartmentName = "Production",
                IsActive = true
            },
            new()
            {
                EmployeeId = "emp-002",
                EmployeeNo = "EMP002",
                FirstName = "Priya",
                MiddleName = "Kumari",
                LastName = "Nair",
                DOB = new DateOnly(1992, 8, 22),
                JoiningDate = new DateOnly(2022, 1, 15),
                AadhaarNo = "873456123456",
                PhoneNo = "+91 98402 34567",
                Gender = "Female",
                DepartmentId = "dept-hr-003",
                DepartmentName = "Human Resources",
                IsActive = true
            },
            new()
            {
                EmployeeId = "emp-003",
                EmployeeNo = "EMP003",
                FirstName = "Amit",
                LastName = "Verma",
                DOB = new DateOnly(1985, 11, 5),
                JoiningDate = new DateOnly(2019, 7, 1),
                AadhaarNo = "764567234567",
                PhoneNo = "+91 98503 45678",
                Gender = "Male",
                DepartmentId = "dept-prod-001",
                DepartmentName = "Production",
                IsActive = true
            },
            new()
            {
                EmployeeId = "emp-004",
                EmployeeNo = "EMP004",
                FirstName = "Sunita",
                LastName = "Patel",
                DOB = new DateOnly(1990, 2, 18),
                JoiningDate = new DateOnly(2020, 9, 20),
                AadhaarNo = "655678345678",
                PhoneNo = "+91 98604 56789",
                Gender = "Female",
                DepartmentId = "dept-admin-002",
                DepartmentName = "Administration",
                IsActive = true
            },
            new()
            {
                EmployeeId = "emp-005",
                EmployeeNo = "EMP005",
                FirstName = "Vikram",
                LastName = "Singh",
                DOB = new DateOnly(1983, 6, 30),
                JoiningDate = new DateOnly(2018, 5, 12),
                AadhaarNo = "546789456789",
                PhoneNo = "+91 98705 67890",
                Gender = "Male",
                DepartmentId = "dept-sec-004",
                DepartmentName = "Security",
                IsActive = true
            },
            new()
            {
                EmployeeId = "emp-006",
                EmployeeNo = "EMP006",
                FirstName = "Ananya",
                LastName = "Dasgupta",
                DOB = new DateOnly(1994, 12, 10),
                JoiningDate = new DateOnly(2023, 2, 1),
                AadhaarNo = "437890567890",
                PhoneNo = "+91 98806 78901",
                Gender = "Female",
                DepartmentId = "dept-admin-002",
                DepartmentName = "Administration",
                IsActive = true
            },
            new()
            {
                EmployeeId = "emp-007",
                EmployeeNo = "EMP007",
                FirstName = "Manoj",
                LastName = "Gupta",
                DOB = new DateOnly(1989, 9, 25),
                JoiningDate = new DateOnly(2021, 11, 15),
                AadhaarNo = "328901678901",
                PhoneNo = "+91 98907 89012",
                Gender = "Male",
                DepartmentId = "dept-prod-001",
                DepartmentName = "Production",
                IsActive = true
            },
            new()
            {
                EmployeeId = "emp-008",
                EmployeeNo = "EMP008",
                FirstName = "Deepak",
                MiddleName = "Kumar",
                LastName = "Yadav",
                DOB = new DateOnly(1991, 3, 14),
                JoiningDate = new DateOnly(2022, 6, 1),
                AadhaarNo = "219012789012",
                PhoneNo = "+91 98108 90123",
                Gender = "Male",
                DepartmentId = "dept-sec-004",
                DepartmentName = "Security",
                IsActive = true
            },
            new()
            {
                EmployeeId = "emp-009",
                EmployeeNo = "EMP009",
                FirstName = "Kavita",
                LastName = "Chauhan",
                DOB = new DateOnly(1996, 7, 7),
                JoiningDate = new DateOnly(2023, 8, 10),
                AadhaarNo = "109123890123",
                PhoneNo = "+91 98209 01234",
                Gender = "Female",
                DepartmentId = "dept-hr-003",
                DepartmentName = "Human Resources",
                IsActive = true
            },
            new()
            {
                EmployeeId = "emp-010",
                EmployeeNo = "EMP010",
                FirstName = "Suresh",
                LastName = "Mukherjee",
                DOB = new DateOnly(1982, 10, 19),
                JoiningDate = new DateOnly(2017, 4, 1),
                AadhaarNo = "998234901234",
                PhoneNo = "+91 98310 12345",
                Gender = "Male",
                DepartmentId = "dept-prod-001",
                DepartmentName = "Production",
                IsActive = true
            }
        };
    }
}

