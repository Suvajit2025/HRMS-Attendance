using HRMS.Models;

namespace HRMS.Interfaces;

public interface IWeeklyOffRepository
{
    Task<IReadOnlyList<WeeklyOffModel>> GetAllAsync();
    Task<WeeklyOffModel?> GetByIdAsync(string weeklyOffId);
    Task AddAsync(WeeklyOffModel weeklyOff);
    Task UpdateAsync(WeeklyOffModel weeklyOff);
    Task DeleteAsync(string weeklyOffId);
}

