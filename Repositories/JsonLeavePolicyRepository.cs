using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Repositories;

public class JsonLeavePolicyRepository : ILeavePolicyRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonLeavePolicyRepository> _logger;

    public JsonLeavePolicyRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonLeavePolicyRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LeavePolicyModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.LeavePoliciesFilePath))
        {
            var seed = GetSeedLeavePolicies();
            await _storage.WriteAsync(_pathProvider.LeavePoliciesFilePath, seed);
            return seed.OrderBy(p => p.PolicyName).ToList();
        }

        var list = await _storage.ReadListAsync<LeavePolicyModel>(_pathProvider.LeavePoliciesFilePath);
        return list.OrderBy(p => p.PolicyName).ToList();
    }

    public async Task<LeavePolicyModel?> GetByIdAsync(string leavePolicyId)
    {
        var list = await GetAllAsync();
        return list.FirstOrDefault(p => string.Equals(p.LeavePolicyId, leavePolicyId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<LeavePolicyModel>> GetByLeaveTypeIdAsync(string leaveTypeId)
    {
        var list = await GetAllAsync();
        return list.Where(p => string.Equals(p.LeaveTypeId, leaveTypeId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task AddAsync(LeavePolicyModel policy)
    {
        var list = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(policy.LeavePolicyId))
        {
            policy.LeavePolicyId = Guid.NewGuid().ToString();
        }
        policy.CreatedDate = DateTime.UtcNow;
        list.Add(policy);
        await _storage.WriteAsync(_pathProvider.LeavePoliciesFilePath, list);
        _logger.LogInformation("Added leave policy: {Name}", policy.PolicyName);
    }

    public async Task UpdateAsync(LeavePolicyModel policy)
    {
        var list = (await GetAllAsync()).ToList();
        var idx = list.FindIndex(p => string.Equals(p.LeavePolicyId, policy.LeavePolicyId, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            list[idx] = policy;
            await _storage.WriteAsync(_pathProvider.LeavePoliciesFilePath, list);
            _logger.LogInformation("Updated leave policy: {Name}", policy.PolicyName);
        }
    }

    public async Task DeleteAsync(string leavePolicyId)
    {
        var list = (await GetAllAsync()).ToList();
        var removed = list.RemoveAll(p => string.Equals(p.LeavePolicyId, leavePolicyId, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            await _storage.WriteAsync(_pathProvider.LeavePoliciesFilePath, list);
            _logger.LogInformation("Deleted leave policy ID: {Id}", leavePolicyId);
        }
    }

    private static List<LeavePolicyModel> GetSeedLeavePolicies()
    {
        return new List<LeavePolicyModel>
        {
            new()
            {
                PolicyName = "Standard Casual Leave Policy",
                LeaveTypeId = "CL",
                AnnualEntitlement = 12.0,
                AccrualFrequency = "Monthly",
                AccrualAmount = 1.0,
                MaximumBalance = 12.0,
                CarryForwardAllowed = false,
                ExcludeWeeklyOffs = true,
                ExcludeHolidays = true,
                EnableSandwichRule = false,
                IsActive = true
            },
            new()
            {
                PolicyName = "Standard Sick Leave Policy",
                LeaveTypeId = "SL",
                AnnualEntitlement = 12.0,
                AccrualFrequency = "Monthly",
                AccrualAmount = 1.0,
                MaximumBalance = 24.0,
                CarryForwardAllowed = true,
                MaximumCarryForward = 10.0,
                ExcludeWeeklyOffs = true,
                ExcludeHolidays = true,
                EnableSandwichRule = false,
                IsActive = true
            },
            new()
            {
                PolicyName = "Standard Earned Leave Policy",
                LeaveTypeId = "EL",
                AnnualEntitlement = 15.0,
                AccrualFrequency = "Annual",
                AccrualAmount = 15.0,
                MaximumBalance = 30.0,
                CarryForwardAllowed = true,
                MaximumCarryForward = 30.0,
                ExcludeWeeklyOffs = true,
                ExcludeHolidays = true,
                EnableSandwichRule = false,
                IsActive = true
            },
            new()
            {
                PolicyName = "Standard Leave Without Pay Policy",
                LeaveTypeId = "LWP",
                AnnualEntitlement = 0.0,
                AccrualFrequency = "Annual",
                AccrualAmount = 0.0,
                MaximumBalance = 365.0,
                AllowNegativeBalance = true,
                ExcludeWeeklyOffs = true,
                ExcludeHolidays = true,
                EnableSandwichRule = false,
                IsActive = true
            }
        };
    }
}

