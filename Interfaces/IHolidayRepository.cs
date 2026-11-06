using HRMS.Models;

namespace HRMS.Interfaces;

public interface IHolidayRepository
{
    Task<IReadOnlyList<HolidayModel>> GetAllAsync();
    Task<HolidayModel?> GetByIdAsync(string holidayId);
    Task<IReadOnlyList<HolidayModel>> GetByYearAsync(int year);
    Task AddAsync(HolidayModel holiday);
    Task UpdateAsync(HolidayModel holiday);
    Task DeleteAsync(string holidayId);
}

