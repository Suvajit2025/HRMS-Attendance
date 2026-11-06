using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Repositories;

public class JsonLeaveBalanceRepository : ILeaveBalanceRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonLeaveBalanceRepository> _logger;

    public JsonLeaveBalanceRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonLeaveBalanceRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmployeeLeaveBalanceModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.EmployeeLeaveBalancesFilePath))
        {
            return new List<EmployeeLeaveBalanceModel>();
        }
        return await _storage.ReadListAsync<EmployeeLeaveBalanceModel>(_pathProvider.EmployeeLeaveBalancesFilePath);
    }

    public async Task<IReadOnlyList<EmployeeLeaveBalanceModel>> GetByEmployeeIdAsync(string employeeId, int financialYear)
    {
        var all = await GetAllAsync();
        return all.Where(b => string.Equals(b.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase) && b.FinancialYear == financialYear).ToList();
    }

    public async Task<EmployeeLeaveBalanceModel?> GetByEmployeeAndLeaveTypeAsync(string employeeId, string leaveTypeId, int financialYear)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(b =>
            string.Equals(b.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(b.LeaveTypeId, leaveTypeId, StringComparison.OrdinalIgnoreCase) &&
            b.FinancialYear == financialYear);
    }

    public async Task AddAsync(EmployeeLeaveBalanceModel balance)
    {
        var list = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(balance.LeaveBalanceId))
        {
            balance.LeaveBalanceId = Guid.NewGuid().ToString();
        }
        balance.LastUpdatedDate = DateTime.UtcNow;
        list.Add(balance);
        await _storage.WriteAsync(_pathProvider.EmployeeLeaveBalancesFilePath, list);
        _logger.LogInformation("Added leave balance for Emp: {EmpId}, Type: {Type}", balance.EmployeeId, balance.LeaveTypeId);
    }

    public async Task UpdateAsync(EmployeeLeaveBalanceModel balance)
    {
        var list = (await GetAllAsync()).ToList();
        var idx = list.FindIndex(b => string.Equals(b.LeaveBalanceId, balance.LeaveBalanceId, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            balance.LastUpdatedDate = DateTime.UtcNow;
            list[idx] = balance;
            await _storage.WriteAsync(_pathProvider.EmployeeLeaveBalancesFilePath, list);
        }
    }

    public async Task DeleteAsync(string leaveBalanceId)
    {
        var list = (await GetAllAsync()).ToList();
        var removed = list.RemoveAll(b => string.Equals(b.LeaveBalanceId, leaveBalanceId, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            await _storage.WriteAsync(_pathProvider.EmployeeLeaveBalancesFilePath, list);
        }
    }
}

