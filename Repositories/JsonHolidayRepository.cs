using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Repositories;

public class JsonHolidayRepository : IHolidayRepository
{
    private readonly JsonFileStorage _storage;
    private readonly StoragePathProvider _pathProvider;
    private readonly ILogger<JsonHolidayRepository> _logger;

    public JsonHolidayRepository(
        JsonFileStorage storage,
        StoragePathProvider pathProvider,
        ILogger<JsonHolidayRepository> logger)
    {
        _storage = storage;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HolidayModel>> GetAllAsync()
    {
        if (!File.Exists(_pathProvider.HolidaysFilePath))
        {
            var seed = GetSeedHolidays();
            await _storage.WriteAsync(_pathProvider.HolidaysFilePath, seed);
            return seed.OrderBy(h => h.HolidayDate).ToList();
        }

        var list = await _storage.ReadListAsync<HolidayModel>(_pathProvider.HolidaysFilePath);
        return list.OrderBy(h => h.HolidayDate).ToList();
    }

    public async Task<HolidayModel?> GetByIdAsync(string holidayId)
    {
        var list = await GetAllAsync();
        return list.FirstOrDefault(h => string.Equals(h.HolidayId, holidayId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<HolidayModel>> GetByYearAsync(int year)
    {
        var list = await GetAllAsync();
        return list.Where(h => h.HolidayDate.Year == year).OrderBy(h => h.HolidayDate).ToList();
    }

    public async Task AddAsync(HolidayModel holiday)
    {
        var list = (await GetAllAsync()).ToList();
        if (string.IsNullOrEmpty(holiday.HolidayId))
        {
            holiday.HolidayId = Guid.NewGuid().ToString();
        }
        holiday.CreatedDate = DateTime.UtcNow;
        list.Add(holiday);
        await _storage.WriteAsync(_pathProvider.HolidaysFilePath, list);
        _logger.LogInformation("Added holiday: {HolidayName} on {Date}", holiday.HolidayName, holiday.HolidayDate);
    }

    public async Task UpdateAsync(HolidayModel holiday)
    {
        var list = (await GetAllAsync()).ToList();
        var idx = list.FindIndex(h => string.Equals(h.HolidayId, holiday.HolidayId, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            list[idx] = holiday;
            await _storage.WriteAsync(_pathProvider.HolidaysFilePath, list);
            _logger.LogInformation("Updated holiday: {HolidayName}", holiday.HolidayName);
        }
    }

    public async Task DeleteAsync(string holidayId)
    {
        var list = (await GetAllAsync()).ToList();
        var removed = list.RemoveAll(h => string.Equals(h.HolidayId, holidayId, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            await _storage.WriteAsync(_pathProvider.HolidaysFilePath, list);
            _logger.LogInformation("Deleted holiday ID: {HolidayId}", holidayId);
        }
    }

    private static List<HolidayModel> GetSeedHolidays()
    {
        int year = DateTime.Today.Year;
        return new List<HolidayModel>
        {
            new() { HolidayName = "New Year's Day", HolidayDate = new DateOnly(year, 1, 1), HolidayType = "Public", IsPaidHoliday = true },
            new() { HolidayName = "Republic Day", HolidayDate = new DateOnly(year, 1, 26), HolidayType = "National", IsPaidHoliday = true },
            new() { HolidayName = "May Day / Labour Day", HolidayDate = new DateOnly(year, 5, 1), HolidayType = "Public", IsPaidHoliday = true },
            new() { HolidayName = "Independence Day", HolidayDate = new DateOnly(year, 8, 15), HolidayType = "National", IsPaidHoliday = true },
            new() { HolidayName = "Gandhi Jayanti", HolidayDate = new DateOnly(year, 10, 2), HolidayType = "National", IsPaidHoliday = true },
            new() { HolidayName = "Diwali (Deepavali)", HolidayDate = new DateOnly(year, 11, 8), HolidayType = "Festival", IsPaidHoliday = true },
            new() { HolidayName = "Christmas Day", HolidayDate = new DateOnly(year, 12, 25), HolidayType = "Festival", IsPaidHoliday = true }
        };
    }
}

