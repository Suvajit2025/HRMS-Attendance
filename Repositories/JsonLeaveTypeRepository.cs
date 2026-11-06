using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Repositories;

public class JsonLeaveTypeRepository : ILeaveTypeRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonLeaveTypeRepository> _logger;

    public JsonLeaveTypeRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonLeaveTypeRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LeaveTypeModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.LeaveTypesFilePath))
        {
            var seed = GetSeedLeaveTypes();
            await _storage.WriteAsync(_pathProvider.LeaveTypesFilePath, seed);
            return seed.OrderBy(l => l.LeaveCode).ToList();
        }

        var list = await _storage.ReadListAsync<LeaveTypeModel>(_pathProvider.LeaveTypesFilePath);
        return list.OrderBy(l => l.LeaveCode).ToList();
    }

    public async Task<LeaveTypeModel?> GetByIdAsync(string leaveTypeId)
    {
        var list = await GetAllAsync();
        return list.FirstOrDefault(l => string.Equals(l.LeaveTypeId, leaveTypeId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<LeaveTypeModel?> GetByCodeAsync(string leaveCode)
    {
        var list = await GetAllAsync();
        return list.FirstOrDefault(l => string.Equals(l.LeaveCode, leaveCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(LeaveTypeModel leaveType)
    {
        var list = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(leaveType.LeaveTypeId))
        {
            leaveType.LeaveTypeId = Guid.NewGuid().ToString();
        }
        leaveType.CreatedDate = DateTime.UtcNow;
        list.Add(leaveType);
        await _storage.WriteAsync(_pathProvider.LeaveTypesFilePath, list);
        _logger.LogInformation("Added leave type: {Code} ({Name})", leaveType.LeaveCode, leaveType.LeaveName);
    }

    public async Task UpdateAsync(LeaveTypeModel leaveType)
    {
        var list = (await GetAllAsync()).ToList();
        var idx = list.FindIndex(l => string.Equals(l.LeaveTypeId, leaveType.LeaveTypeId, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            list[idx] = leaveType;
            await _storage.WriteAsync(_pathProvider.LeaveTypesFilePath, list);
            _logger.LogInformation("Updated leave type: {Code}", leaveType.LeaveCode);
        }
    }

    public async Task DeleteAsync(string leaveTypeId)
    {
        var list = (await GetAllAsync()).ToList();
        var removed = list.RemoveAll(l => string.Equals(l.LeaveTypeId, leaveTypeId, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            await _storage.WriteAsync(_pathProvider.LeaveTypesFilePath, list);
            _logger.LogInformation("Deleted leave type ID: {Id}", leaveTypeId);
        }
    }

    private static List<LeaveTypeModel> GetSeedLeaveTypes()
    {
        return new List<LeaveTypeModel>
        {
            new() { LeaveCode = "CL", LeaveName = "Casual Leave", Description = "Short-notice personal leave", IsPaidLeave = true, IsHalfDayAllowed = true, IsCarryForwardAllowed = false, RequiresApproval = true, RequiresDocument = false },
            new() { LeaveCode = "SL", LeaveName = "Sick Leave", Description = "Medical illness or recovery leave", IsPaidLeave = true, IsHalfDayAllowed = true, IsCarryForwardAllowed = true, MaximumCarryForward = 10, RequiresApproval = true, RequiresDocument = true },
            new() { LeaveCode = "EL", LeaveName = "Earned Leave", Description = "Statutory annual earned leave entitlement", IsPaidLeave = true, IsHalfDayAllowed = false, IsCarryForwardAllowed = true, MaximumCarryForward = 30, RequiresApproval = true, RequiresDocument = false },
            new() { LeaveCode = "PL", LeaveName = "Privilege Leave", Description = "Planned privilege vacation leave", IsPaidLeave = true, IsHalfDayAllowed = true, IsCarryForwardAllowed = true, MaximumCarryForward = 15, RequiresApproval = true, RequiresDocument = false },
            new() { LeaveCode = "LWP", LeaveName = "Leave Without Pay", Description = "Loss of pay / unpaid absence", IsPaidLeave = false, IsHalfDayAllowed = true, IsCarryForwardAllowed = false, RequiresApproval = true, RequiresDocument = false },
            new() { LeaveCode = "CO", LeaveName = "Compensatory Off", Description = "Compensatory leave for weekend/holiday shift", IsPaidLeave = true, IsHalfDayAllowed = true, IsCarryForwardAllowed = false, RequiresApproval = true, RequiresDocument = false }
        };
    }
}

