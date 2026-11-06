using HRMS.Models;

namespace HRMS.Interfaces;

public interface IAttendanceRepository
{
    Task<IReadOnlyList<AttendanceModel>> GetAllAsync();
    Task<IReadOnlyList<AttendanceModel>> GetByDateAsync(DateOnly date);
    Task<IReadOnlyList<AttendanceModel>> GetDateRangeAsync(DateOnly startDate, DateOnly endDate);
    Task<IReadOnlyList<AttendanceModel>> GetByEmployeeIdAsync(string employeeId, DateOnly? startDate = null, DateOnly? endDate = null);
    Task<IReadOnlyList<AttendanceModel>> GetEmployeePunchesTodayAsync(string employeeId, DateOnly date);
    Task<AttendanceModel?> GetLatestPunchTodayAsync(string employeeId, DateOnly date);
    Task AddAsync(AttendanceModel attendance);
}

