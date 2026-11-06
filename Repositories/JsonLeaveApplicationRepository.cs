using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Repositories;

public class JsonLeaveApplicationRepository : ILeaveApplicationRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonLeaveApplicationRepository> _logger;

    public JsonLeaveApplicationRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonLeaveApplicationRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LeaveApplicationModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.LeaveApplicationsFilePath))
        {
            return new List<LeaveApplicationModel>();
        }
        var list = await _storage.ReadListAsync<LeaveApplicationModel>(_pathProvider.LeaveApplicationsFilePath);
        return list.OrderByDescending(a => a.ApplicationDate).ToList();
    }

    public async Task<LeaveApplicationModel?> GetByIdAsync(string applicationId)
    {
        var list = await GetAllAsync();
        return list.FirstOrDefault(a => string.Equals(a.LeaveApplicationId, applicationId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<LeaveApplicationModel>> GetByEmployeeIdAsync(string employeeId)
    {
        var list = await GetAllAsync();
        return list.Where(a => string.Equals(a.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase))
                   .OrderByDescending(a => a.FromDate).ToList();
    }

    public async Task<IReadOnlyList<LeaveApplicationModel>> GetPendingApplicationsAsync()
    {
        var list = await GetAllAsync();
        return list.Where(a => string.Equals(a.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                   .OrderBy(a => a.FromDate).ToList();
    }

    public async Task AddAsync(LeaveApplicationModel application)
    {
        var list = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(application.LeaveApplicationId))
        {
            application.LeaveApplicationId = Guid.NewGuid().ToString();
        }
        application.ApplicationDate = DateTime.UtcNow;
        list.Add(application);
        await _storage.WriteAsync(_pathProvider.LeaveApplicationsFilePath, list);
        _logger.LogInformation("Added leave application: Emp {Emp}, From {From} To {To}", application.EmployeeId, application.FromDate, application.ToDate);
    }

    public async Task UpdateAsync(LeaveApplicationModel application)
    {
        var list = (await GetAllAsync()).ToList();
        var idx = list.FindIndex(a => string.Equals(a.LeaveApplicationId, application.LeaveApplicationId, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            list[idx] = application;
            await _storage.WriteAsync(_pathProvider.LeaveApplicationsFilePath, list);
            _logger.LogInformation("Updated leave application: {Id}, Status {Status}", application.LeaveApplicationId, application.Status);
        }
    }

    public async Task DeleteAsync(string applicationId)
    {
        var list = (await GetAllAsync()).ToList();
        var removed = list.RemoveAll(a => string.Equals(a.LeaveApplicationId, applicationId, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            await _storage.WriteAsync(_pathProvider.LeaveApplicationsFilePath, list);
        }
    }
}

