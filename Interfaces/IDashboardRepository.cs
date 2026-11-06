using HRMS.Models;

namespace HRMS.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardModel> GetDashboardStatsAsync(DateOnly date, string? departmentId = null, string? searchTerm = null);
}

