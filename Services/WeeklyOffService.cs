using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Services;

public class WeeklyOffService : IWeeklyOffService
{
    private readonly IWeeklyOffRepository _weeklyOffRepo;
    private readonly ILogger<WeeklyOffService> _logger;

    public WeeklyOffService(IWeeklyOffRepository weeklyOffRepo, ILogger<WeeklyOffService> logger)
    {
        _weeklyOffRepo = weeklyOffRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WeeklyOffModel>> GetAllAsync()
    {
        return await _weeklyOffRepo.GetAllAsync();
    }

    public async Task<WeeklyOffModel?> GetByIdAsync(string weeklyOffId)
    {
        return await _weeklyOffRepo.GetByIdAsync(weeklyOffId);
    }

    public async Task<(bool Success, string? ErrorMessage)> AddAsync(WeeklyOffModel weeklyOff)
    {
        var all = await _weeklyOffRepo.GetAllAsync();
        var exists = all.Any(w =>
            w.DayOfWeek == weeklyOff.DayOfWeek &&
            string.Equals(w.DepartmentId ?? "", weeklyOff.DepartmentId ?? "", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(w.LocationCode ?? "", weeklyOff.LocationCode ?? "", StringComparison.OrdinalIgnoreCase) &&
            w.AlternateWeekNumber == weeklyOff.AlternateWeekNumber);

        if (exists)
        {
            return (false, "A weekly off configuration already exists for this day and scope.");
        }

        await _weeklyOffRepo.AddAsync(weeklyOff);
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(WeeklyOffModel weeklyOff)
    {
        var existing = await _weeklyOffRepo.GetByIdAsync(weeklyOff.WeeklyOffId);
        if (existing == null)
        {
            return (false, "Weekly off configuration not found.");
        }

        await _weeklyOffRepo.UpdateAsync(weeklyOff);
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(string weeklyOffId)
    {
        var existing = await _weeklyOffRepo.GetByIdAsync(weeklyOffId);
        if (existing == null)
        {
            return (false, "Weekly off configuration not found.");
        }

        await _weeklyOffRepo.DeleteAsync(weeklyOffId);
        return (true, null);
    }

    public async Task<bool> IsWeeklyOffAsync(DateOnly date, string? departmentId, string? locationCode)
    {
        var all = await _weeklyOffRepo.GetAllAsync();
        var active = all.Where(w =>
            w.IsActive &&
            w.IsWeeklyOff &&
            w.DayOfWeek == date.DayOfWeek &&
            w.EffectiveFrom <= date &&
            (!w.EffectiveTo.HasValue || w.EffectiveTo.Value >= date)).ToList();

        if (active.Count == 0)
        {
            // Default fallback: Sunday is weekly off if nothing configured
            return date.DayOfWeek == DayOfWeek.Sunday;
        }

        // 1. Department-specific match
        if (!string.IsNullOrEmpty(departmentId))
        {
            var deptMatch = active.FirstOrDefault(w => string.Equals(w.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase));
            if (deptMatch != null)
            {
                return MatchesWeekNumber(deptMatch, date);
            }
        }

        // 2. Location-specific match
        if (!string.IsNullOrEmpty(locationCode))
        {
            var locMatch = active.FirstOrDefault(w => string.Equals(w.LocationCode, locationCode, StringComparison.OrdinalIgnoreCase));
            if (locMatch != null)
            {
                return MatchesWeekNumber(locMatch, date);
            }
        }

        // 3. Organization-wide match (both null)
        var orgMatch = active.FirstOrDefault(w => string.IsNullOrEmpty(w.DepartmentId) && string.IsNullOrEmpty(w.LocationCode));
        if (orgMatch != null)
        {
            return MatchesWeekNumber(orgMatch, date);
        }

        return false;
    }

    private static bool MatchesWeekNumber(WeeklyOffModel config, DateOnly date)
    {
        if (!config.IsAlternateWeek)
        {
            return true;
        }

        int weekNumber = (date.Day - 1) / 7 + 1;
        return config.AlternateWeekNumber.HasValue && config.AlternateWeekNumber.Value == weekNumber;
    }
}

