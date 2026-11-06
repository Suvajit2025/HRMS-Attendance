using HRMS.Models;

namespace HRMS.Interfaces;

public interface IWeeklyOffService
{
    Task<IReadOnlyList<WeeklyOffModel>> GetAllAsync();
    Task<WeeklyOffModel?> GetByIdAsync(string weeklyOffId);
    Task<(bool Success, string? ErrorMessage)> AddAsync(WeeklyOffModel weeklyOff);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(WeeklyOffModel weeklyOff);
    Task<(bool Success, string? ErrorMessage)> DeleteAsync(string weeklyOffId);
    Task<bool> IsWeeklyOffAsync(DateOnly date, string? departmentId, string? locationCode);
}

