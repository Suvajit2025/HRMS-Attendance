using System.Text;
using HRMS.Interfaces;
using HRMS.Models;

namespace HRMS.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepo;

    public DashboardService(IDashboardRepository dashboardRepo)
    {
        _dashboardRepo = dashboardRepo;
    }

    public async Task<DashboardModel> GetDashboardDataAsync(DateOnly? date = null, string? departmentId = null, string? searchTerm = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        return await _dashboardRepo.GetDashboardStatsAsync(targetDate, departmentId, searchTerm);
    }

    public async Task<byte[]> GenerateAttendanceCsvAsync(DateOnly date, string? departmentId = null)
    {
        var stats = await _dashboardRepo.GetDashboardStatsAsync(date, departmentId);

        var sb = new StringBuilder();
        sb.AppendLine("HRMS Attendance Report");
        sb.AppendLine($"Report Date: {date:yyyy-MM-dd}");
        sb.AppendLine($"Generated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Total Active Employees: {stats.TotalEmployees}");
        sb.AppendLine($"Present: {stats.PresentToday}, Absent: {stats.AbsentToday}, Inside: {stats.CurrentlyInside}, Late: {stats.LateArrivals}");
        sb.AppendLine();
        sb.AppendLine("Employee No,Employee Name,Department,First IN,Last OUT,Working Hours,Attendance Status");

        foreach (var row in stats.TodayAttendanceRecords)
        {
            var firstIn = row.FirstInTime.HasValue ? row.FirstInTime.Value.ToString("hh:mm tt") : "--";
            var lastOut = row.LastOutTime.HasValue ? row.LastOutTime.Value.ToString("hh:mm tt") : "--";
            sb.AppendLine($"\"{EscapeCsv(row.EmployeeNo)}\",\"{EscapeCsv(row.EmployeeName)}\",\"{EscapeCsv(row.DepartmentName ?? "")}\",\"{firstIn}\",\"{lastOut}\",\"{row.WorkingHoursFormatted}\",\"{row.AttendanceStatus}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string val)
    {
        return val.Replace("\"", "\"\"");
    }
}

