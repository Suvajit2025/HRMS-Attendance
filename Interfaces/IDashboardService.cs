using HRMS.Models;

namespace HRMS.Interfaces;

public interface IDashboardService
{
    Task<DashboardModel> GetDashboardDataAsync(DateOnly? date = null, string? departmentId = null, string? searchTerm = null);
    Task<byte[]> GenerateAttendanceCsvAsync(DateOnly date, string? departmentId = null);
}

