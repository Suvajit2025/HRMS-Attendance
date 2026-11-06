using HRMS.Models;

namespace HRMS.Interfaces;

public interface IHolidayService
{
    Task<IReadOnlyList<HolidayModel>> GetHolidaysAsync(int? year, string? type, string? departmentId, string? locationCode);
    Task<HolidayModel?> GetByIdAsync(string holidayId);
    Task<(bool Success, string? ErrorMessage, HolidayModel? Holiday)> CreateHolidayAsync(HolidayModel holiday);
    Task<(bool Success, string? ErrorMessage)> UpdateHolidayAsync(HolidayModel holiday);
    Task<(bool Success, string? ErrorMessage)> DeleteHolidayAsync(string holidayId);
    Task<bool> IsHolidayAsync(DateOnly date, string? departmentId, string? locationCode);
    Task<HolidayModel?> GetHolidayForDateAsync(DateOnly date, string? departmentId, string? locationCode);
    Task<IReadOnlyList<HolidayModel>> GetUpcomingHolidaysAsync(int count = 5);
}

