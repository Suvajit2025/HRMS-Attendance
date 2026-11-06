using HRMS.Interfaces;
using HRMS.Models;
using HRMS.ViewModels;

namespace HRMS.Services;

public class ResolvedDayAttendance
{
    public DateOnly Date { get; set; }
    public string StatusCode { get; set; } = string.Empty; // P, U, A, H, or "" (Blank)
    public string StatusName { get; set; } = string.Empty;
    public string CssClass { get; set; } = "code-blank";
    public string TooltipText { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
    public bool IsPaidLeave { get; set; }
    public bool IsUnpaidLeave { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsWeeklyOff { get; set; }
    public bool IsAbsent { get; set; }
    public bool IsBeforeJoining { get; set; }
    public bool IsFutureDate { get; set; }
    public bool IsWorkedOnHolidayOrLeave { get; set; }
    public string? LeaveApplicationId { get; set; }
    public bool IsOnLeave => IsPaidLeave || IsUnpaidLeave;
    public string? PunchInTime { get; set; }
    public string? PunchOutTime { get; set; }
}

public interface IAttendanceCalendarResolver
{
    Task<ResolvedDayAttendance> ResolveDailyStatusAsync(
        EmployeeModel employee,
        DateOnly date,
        IReadOnlyList<AttendanceModel> dayPunches,
        IReadOnlyList<LeaveApplicationModel> approvedLeaves,
        IReadOnlyList<LeaveTypeModel> leaveTypes,
        DateOnly today);

    Task<ResolvedDayAttendance> ResolveDailyStatusAsync(
        string employeeId,
        DateOnly date);
}

public class AttendanceCalendarResolver : IAttendanceCalendarResolver
{
    private readonly IHolidayService _holidayService;
    private readonly IWeeklyOffService _weeklyOffService;
    private readonly ILeaveService? _leaveService;
    private readonly ILeaveTypeRepository? _leaveTypeRepo;
    private readonly IEmployeeRepository? _empRepo;
    private readonly IAttendanceRepository? _attRepo;

    public AttendanceCalendarResolver(
        IHolidayService holidayService,
        IWeeklyOffService weeklyOffService,
        ILeaveService? leaveService = null,
        ILeaveTypeRepository? leaveTypeRepo = null,
        IEmployeeRepository? empRepo = null,
        IAttendanceRepository? attRepo = null)
    {
        _holidayService = holidayService;
        _weeklyOffService = weeklyOffService;
        _leaveService = leaveService;
        _leaveTypeRepo = leaveTypeRepo;
        _empRepo = empRepo;
        _attRepo = attRepo;
    }

    public async Task<ResolvedDayAttendance> ResolveDailyStatusAsync(
        string employeeId,
        DateOnly date)
    {
        EmployeeModel? emp = null;
        if (_empRepo != null)
        {
            emp = await _empRepo.GetByIdAsync(employeeId) ?? await _empRepo.GetByEmployeeNoAsync(employeeId);
        }
        emp ??= new EmployeeModel { EmployeeId = employeeId, FirstName = employeeId, LastName = "" };

        var punches = _attRepo != null
            ? (await _attRepo.GetEmployeePunchesTodayAsync(emp.EmployeeId, date)).ToList()
            : new List<AttendanceModel>();

        var leaves = _leaveService != null
            ? (await _leaveService.GetApprovedLeavesForEmployeeAsync(emp.EmployeeId, date, date)).ToList()
            : new List<LeaveApplicationModel>();

        var types = _leaveTypeRepo != null
            ? (await _leaveTypeRepo.GetAllAsync()).ToList()
            : new List<LeaveTypeModel>();

        return await ResolveDailyStatusAsync(emp, date, punches, leaves, types, DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<ResolvedDayAttendance> ResolveDailyStatusAsync(
        EmployeeModel employee,
        DateOnly date,
        IReadOnlyList<AttendanceModel> dayPunches,
        IReadOnlyList<LeaveApplicationModel> approvedLeaves,
        IReadOnlyList<LeaveTypeModel> leaveTypes,
        DateOnly today)
    {
        var result = new ResolvedDayAttendance { Date = date };

        // 1. Check if prior to employee joining date
        if (employee.JoiningDate != default && date < employee.JoiningDate)
        {
            result.StatusCode = "";
            result.StatusName = "Before Joining Date";
            result.CssClass = "code-blank";
            result.TooltipText = $"{employee.FullName} - Joined on {employee.JoiningDate:dd MMM yyyy} (Before Joining)";
            result.IsBeforeJoining = true;
            return result;
        }

        // 2. Check if future calendar date
        if (date > today)
        {
            result.StatusCode = "";
            result.StatusName = "Future Date";
            result.CssClass = "code-blank";
            result.TooltipText = $"{date:dd MMM yyyy} (Future Date)";
            result.IsFutureDate = true;
            return result;
        }

        // 3. Check for actual physical punch event
        bool hasPunches = dayPunches != null && dayPunches.Count > 0;
        var punchIn = dayPunches?.FirstOrDefault(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase));
        var punchOut = dayPunches?.FirstOrDefault(p => string.Equals(p.PunchType, "OUT", StringComparison.OrdinalIgnoreCase));
        result.PunchInTime = punchIn?.PunchTime.ToString("hh:mm tt");
        result.PunchOutTime = punchOut?.PunchTime.ToString("hh:mm tt");

        // 4. Check for Approved Leave on this date
        var leaveApp = approvedLeaves?.FirstOrDefault(l => l.FromDate <= date && l.ToDate >= date);
        LeaveTypeModel? leaveType = null;
        if (leaveApp != null)
        {
            leaveType = leaveTypes?.FirstOrDefault(t => string.Equals(t.LeaveTypeId, leaveApp.LeaveTypeId, StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(t.LeaveCode, leaveApp.LeaveTypeId, StringComparison.OrdinalIgnoreCase));
        }

        // 5. Check for Public Holiday & Weekly Off
        var holiday = await _holidayService.GetHolidayForDateAsync(date, employee.DepartmentId, null);
        bool isWeeklyOff = await _weeklyOffService.IsWeeklyOffAsync(date, employee.DepartmentId, null);

        // PRIORITY RESOLUTION STRATEGY:
        // Priority 1: Actual attendance punches exist
        if (hasPunches)
        {
            result.IsPresent = true;
            result.StatusCode = "P";
            result.StatusName = "Present";
            result.CssClass = "code-p";

            if (holiday != null)
            {
                result.IsWorkedOnHolidayOrLeave = true;
                result.TooltipText = $"{employee.FullName} - Attended on Holiday ({holiday.HolidayName}) | IN: {result.PunchInTime ?? "--"}, OUT: {result.PunchOutTime ?? "--"}";
            }
            else if (isWeeklyOff)
            {
                result.IsWorkedOnHolidayOrLeave = true;
                result.TooltipText = $"{employee.FullName} - Attended on Weekly Off | IN: {result.PunchInTime ?? "--"}, OUT: {result.PunchOutTime ?? "--"}";
            }
            else if (leaveApp != null)
            {
                result.IsWorkedOnHolidayOrLeave = true;
                result.TooltipText = $"{employee.FullName} - Attended on Approved Leave ({leaveType?.LeaveName ?? "Leave"}) | IN: {result.PunchInTime ?? "--"}";
            }
            else
            {
                result.TooltipText = $"{employee.FullName} - Present on {date:dd MMM} (IN: {result.PunchInTime ?? "--"}, OUT: {result.PunchOutTime ?? "--"})";
            }

            return result;
        }

        // Priority 2: Approved Leave
        if (leaveApp != null)
        {
            result.LeaveApplicationId = leaveApp.LeaveApplicationId;
            bool isPaid = leaveType?.IsPaidLeave ?? true;
            if (isPaid)
            {
                result.IsPaidLeave = true;
                result.StatusCode = "P";
                result.StatusName = $"Approved Leave ({leaveType?.LeaveCode ?? "Paid"})";
                result.CssClass = "code-p";
                result.TooltipText = $"{employee.FullName} - Approved {leaveType?.LeaveName ?? "Leave"} ({leaveApp.TotalDays} day(s))";
            }
            else
            {
                result.IsUnpaidLeave = true;
                result.StatusCode = "U";
                result.StatusName = $"Unpaid Leave ({leaveType?.LeaveCode ?? "LWP"})";
                result.CssClass = "code-u";
                result.TooltipText = $"{employee.FullName} - Approved Unpaid Leave ({leaveType?.LeaveName ?? "LWP"})";
            }
            return result;
        }

        // Priority 3: Public Holiday
        if (holiday != null)
        {
            result.IsHoliday = true;
            result.StatusCode = "H";
            result.StatusName = $"Holiday ({holiday.HolidayName})";
            result.CssClass = "code-h";
            result.TooltipText = $"{date:dd MMM yyyy} - Holiday: {holiday.HolidayName}";
            return result;
        }

        // Priority 4: Weekly Off
        if (isWeeklyOff)
        {
            result.IsWeeklyOff = true;
            result.StatusCode = "H";
            result.StatusName = "Weekly Off";
            result.CssClass = "code-h";
            result.TooltipText = $"{date:dd MMM yyyy} - Weekly Off";
            return result;
        }

        // Priority 5: Unpunched working day in the past -> Absent (A)
        result.IsAbsent = true;
        result.StatusCode = "A";
        result.StatusName = "Absent";
        result.CssClass = "code-a";
        result.TooltipText = $"{employee.FullName} - Absent on {date:dd MMM yyyy}";
        return result;
    }
}
