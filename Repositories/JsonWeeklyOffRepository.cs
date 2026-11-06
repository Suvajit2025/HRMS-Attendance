using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Repositories;

public class JsonWeeklyOffRepository : IWeeklyOffRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonWeeklyOffRepository> _logger;

    public JsonWeeklyOffRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonWeeklyOffRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WeeklyOffModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.WeeklyOffsFilePath))
        {
            var seed = GetSeedWeeklyOffs();
            await _storage.WriteAsync(_pathProvider.WeeklyOffsFilePath, seed);
            return seed;
        }

        return await _storage.ReadListAsync<WeeklyOffModel>(_pathProvider.WeeklyOffsFilePath);
    }

    public async Task<WeeklyOffModel?> GetByIdAsync(string weeklyOffId)
    {
        var list = await GetAllAsync();
        return list.FirstOrDefault(w => string.Equals(w.WeeklyOffId, weeklyOffId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(WeeklyOffModel weeklyOff)
    {
        var list = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(weeklyOff.WeeklyOffId))
        {
            weeklyOff.WeeklyOffId = Guid.NewGuid().ToString();
        }
        weeklyOff.CreatedDate = DateTime.UtcNow;
        list.Add(weeklyOff);
        await _storage.WriteAsync(_pathProvider.WeeklyOffsFilePath, list);
        _logger.LogInformation("Added weekly off: {Day} (Dept: {Dept})", weeklyOff.DayOfWeek, weeklyOff.DepartmentId);
    }

    public async Task UpdateAsync(WeeklyOffModel weeklyOff)
    {
        var list = (await GetAllAsync()).ToList();
        var idx = list.FindIndex(w => string.Equals(w.WeeklyOffId, weeklyOff.WeeklyOffId, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            list[idx] = weeklyOff;
            await _storage.WriteAsync(_pathProvider.WeeklyOffsFilePath, list);
            _logger.LogInformation("Updated weekly off: {Day}", weeklyOff.DayOfWeek);
        }
    }

    public async Task DeleteAsync(string weeklyOffId)
    {
        var list = (await GetAllAsync()).ToList();
        var removed = list.RemoveAll(w => string.Equals(w.WeeklyOffId, weeklyOffId, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            await _storage.WriteAsync(_pathProvider.WeeklyOffsFilePath, list);
            _logger.LogInformation("Deleted weekly off ID: {WeeklyOffId}", weeklyOffId);
        }
    }

    private static List<WeeklyOffModel> GetSeedWeeklyOffs()
    {
        return new List<WeeklyOffModel>
        {
            new()
            {
                DayOfWeek = DayOfWeek.Sunday,
                IsWeeklyOff = true,
                EffectiveFrom = new DateOnly(2020, 1, 1),
                IsActive = true
            }
        };
    }
}

