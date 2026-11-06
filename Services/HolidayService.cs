using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Services;

public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _holidayRepo;
    private readonly ILogger<HolidayService> _logger;

    public HolidayService(IHolidayRepository holidayRepo, ILogger<HolidayService> logger)
    {
        _holidayRepo = holidayRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HolidayModel>> GetHolidaysAsync(int? year, string? type, string? departmentId, string? locationCode)
    {
        var all = await _holidayRepo.GetAllAsync();
        var query = all.AsEnumerable();

        if (year.HasValue && year.Value > 0)
        {
            query = query.Where(h => h.HolidayDate.Year == year.Value);
        }

        if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(h => string.Equals(h.HolidayType, type, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            query = query.Where(h => string.IsNullOrEmpty(h.DepartmentId) || string.Equals(h.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(locationCode))
        {
            query = query.Where(h => string.IsNullOrEmpty(h.LocationCode) || string.Equals(h.LocationCode, locationCode, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderBy(h => h.HolidayDate).ToList();
    }

    public async Task<HolidayModel?> GetByIdAsync(string holidayId)
    {
        return await _holidayRepo.GetByIdAsync(holidayId);
    }

    public async Task<(bool Success, string? ErrorMessage, HolidayModel? Holiday)> CreateHolidayAsync(HolidayModel holiday)
    {
        if (string.IsNullOrWhiteSpace(holiday.HolidayName))
        {
            return (false, "Holiday name is required.", null);
        }

        if (holiday.HolidayDate == default)
        {
            return (false, "A valid holiday date is required.", null);
        }

        // Duplicate validation for same date & applicable scope
        var existing = await _holidayRepo.GetAllAsync();
        var isDuplicate = existing.Any(h =>
            h.HolidayDate == holiday.HolidayDate &&
            string.Equals(h.DepartmentId ?? "", holiday.DepartmentId ?? "", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(h.LocationCode ?? "", holiday.LocationCode ?? "", StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            return (false, $"A holiday already exists on {holiday.HolidayDate:dd MMM yyyy} for the specified scope.", null);
        }

        await _holidayRepo.AddAsync(holiday);
        return (true, null, holiday);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateHolidayAsync(HolidayModel holiday)
    {
        if (string.IsNullOrWhiteSpace(holiday.HolidayName))
        {
            return (false, "Holiday name is required.");
        }

        var existing = await _holidayRepo.GetByIdAsync(holiday.HolidayId);
        if (existing == null)
        {
            return (false, "Holiday record not found.");
        }

        // Duplicate validation
        var all = await _holidayRepo.GetAllAsync();
        var isDuplicate = all.Any(h =>
            h.HolidayId != holiday.HolidayId &&
            h.HolidayDate == holiday.HolidayDate &&
            string.Equals(h.DepartmentId ?? "", holiday.DepartmentId ?? "", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(h.LocationCode ?? "", holiday.LocationCode ?? "", StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            return (false, $"Another holiday already exists on {holiday.HolidayDate:dd MMM yyyy} for this scope.");
        }

        await _holidayRepo.UpdateAsync(holiday);
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteHolidayAsync(string holidayId)
    {
        var existing = await _holidayRepo.GetByIdAsync(holidayId);
        if (existing == null)
        {
            return (false, "Holiday not found.");
        }

        await _holidayRepo.DeleteAsync(holidayId);
        return (true, null);
    }

    public async Task<bool> IsHolidayAsync(DateOnly date, string? departmentId, string? locationCode)
    {
        var holiday = await GetHolidayForDateAsync(date, departmentId, locationCode);
        return holiday != null;
    }

    public async Task<HolidayModel?> GetHolidayForDateAsync(DateOnly date, string? departmentId, string? locationCode)
    {
        var all = await _holidayRepo.GetAllAsync();
        return all.FirstOrDefault(h =>
            h.IsActive &&
            h.HolidayDate == date &&
            (string.IsNullOrEmpty(h.DepartmentId) || string.Equals(h.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(h.LocationCode) || string.Equals(h.LocationCode, locationCode, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<IReadOnlyList<HolidayModel>> GetUpcomingHolidaysAsync(int count = 5)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var all = await _holidayRepo.GetAllAsync();
        return all.Where(h => h.IsActive && h.HolidayDate >= today)
                  .OrderBy(h => h.HolidayDate)
                  .Take(count)
                  .ToList();
    }
}

