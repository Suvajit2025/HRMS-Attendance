using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Logging;

namespace HRMS.Services;

public class LeaveCalculationService : ILeaveCalculationService
{
    private readonly IWeeklyOffService _weeklyOffService;
    private readonly IHolidayService _holidayService;
    private readonly ILeavePolicyRepository _policyRepo;
    private readonly ILogger<LeaveCalculationService> _logger;

    public LeaveCalculationService(
        IWeeklyOffService weeklyOffService,
        IHolidayService holidayService,
        ILeavePolicyRepository policyRepo,
        ILogger<LeaveCalculationService> logger)
    {
        _weeklyOffService = weeklyOffService;
        _holidayService = holidayService;
        _policyRepo = policyRepo;
        _logger = logger;
    }

    public async Task<LeaveDurationCalculationResult> CalculateDurationAsync(
        DateOnly fromDate,
        DateOnly toDate,
        string leaveTypeId,
        string? departmentId,
        string? locationCode,
        bool isHalfDay = false,
        string? halfDaySession = null)
    {
        var result = new LeaveDurationCalculationResult();

        if (toDate < fromDate)
        {
            result.TotalDays = 0;
            result.BreakdownSummary = "Invalid date range: End date is before Start date.";
            return result;
        }

        // 1. Resolve applicable Leave Policy
        var policies = await _policyRepo.GetByLeaveTypeIdAsync(leaveTypeId);
        var policy = policies.FirstOrDefault(p => p.IsActive && !string.IsNullOrEmpty(p.DepartmentId) && string.Equals(p.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase))
                     ?? policies.FirstOrDefault(p => p.IsActive && string.IsNullOrEmpty(p.DepartmentId))
                     ?? new LeavePolicyModel { ExcludeWeeklyOffs = true, ExcludeHolidays = true, EnableSandwichRule = false };

        // 2. Half-Day Leave
        if (isHalfDay)
        {
            // Half-day applies to single date
            bool isWeeklyOff = await _weeklyOffService.IsWeeklyOffAsync(fromDate, departmentId, locationCode);
            bool isHoliday = await _holidayService.IsHolidayAsync(fromDate, departmentId, locationCode);

            if (isWeeklyOff || isHoliday)
            {
                result.TotalDays = 0;
                result.BreakdownSummary = "Selected date is a non-working day or holiday.";
                return result;
            }

            result.CalendarDays = 1;
            result.TotalDays = 0.5;
            result.BillableDates.Add(fromDate);
            result.BreakdownSummary = $"Half Day ({halfDaySession ?? "First Half"}) on {fromDate:dd MMM yyyy} = 0.5 Day";
            return result;
        }

        // 3. Multi-Day Leave Calculation
        int calendarDays = 0;
        int weeklyOffDays = 0;
        int holidayDays = 0;
        int overlapDays = 0;

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            calendarDays++;
            bool isWeeklyOff = await _weeklyOffService.IsWeeklyOffAsync(date, departmentId, locationCode);
            bool isHoliday = await _holidayService.IsHolidayAsync(date, departmentId, locationCode);

            if (isWeeklyOff && isHoliday)
            {
                overlapDays++;
            }
            else if (isWeeklyOff)
            {
                weeklyOffDays++;
            }
            else if (isHoliday)
            {
                holidayDays++;
            }

            bool shouldExclude = false;

            if (policy.EnableSandwichRule)
            {
                // Sandwich rule: internal weekly offs/holidays sandwiched inside leave period are counted as leave
                // But boundary weekly offs/holidays at start or end are excluded
                bool isBoundary = (date == fromDate || date == toDate);
                if (isBoundary && (isWeeklyOff || isHoliday))
                {
                    shouldExclude = true;
                }
            }
            else
            {
                // Standard: exclude weekly offs and holidays
                if (policy.ExcludeWeeklyOffs && isWeeklyOff) shouldExclude = true;
                if (policy.ExcludeHolidays && isHoliday) shouldExclude = true;
            }

            if (!shouldExclude)
            {
                result.BillableDates.Add(date);
            }
        }

        result.CalendarDays = calendarDays;
        result.WeeklyOffDays = weeklyOffDays;
        result.HolidayDays = holidayDays;
        result.OverlapDays = overlapDays;
        result.IsSandwichApplied = policy.EnableSandwichRule;
        result.TotalDays = result.BillableDates.Count;

        var parts = new List<string> { $"{calendarDays} Calendar Day(s)" };
        if (weeklyOffDays > 0) parts.Add($"{weeklyOffDays} Weekly Off(s)");
        if (holidayDays > 0) parts.Add($"{holidayDays} Holiday(s)");
        if (overlapDays > 0) parts.Add($"{overlapDays} Overlapping Holiday/Off(s)");
        result.BreakdownSummary = $"{string.Join(" - ", parts)} = {result.TotalDays} Net Leave Day(s)";

        return result;
    }
}

