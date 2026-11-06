using HRMS.Models;

namespace HRMS.Interfaces;

public class LeaveDurationCalculationResult
{
    public double TotalDays { get; set; }
    public int CalendarDays { get; set; }
    public int WeeklyOffDays { get; set; }
    public int HolidayDays { get; set; }
    public int OverlapDays { get; set; }
    public bool IsSandwichApplied { get; set; }
    public List<DateOnly> BillableDates { get; set; } = new();
    public string BreakdownSummary { get; set; } = string.Empty;
}

public interface ILeaveCalculationService
{
    Task<LeaveDurationCalculationResult> CalculateDurationAsync(
        DateOnly fromDate,
        DateOnly toDate,
        string leaveTypeId,
        string? departmentId,
        string? locationCode,
        bool isHalfDay = false,
        string? halfDaySession = null);
}

