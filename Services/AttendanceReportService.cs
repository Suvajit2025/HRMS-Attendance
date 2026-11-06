using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using HRMS.ViewModels;
using Microsoft.Extensions.Options;

namespace HRMS.Services;

public class AttendanceReportService : IAttendanceReportService
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly JsonStorageOptions _storageOptions;
    private readonly ILogger<AttendanceReportService> _logger;
    private readonly IAttendanceCalendarResolver? _calendarResolver;
    private readonly ILeaveService? _leaveService;
    private readonly ILeaveTypeRepository? _leaveTypeRepo;

    public AttendanceReportService(
        IAttendanceRepository attendanceRepo,
        IEmployeeRepository employeeRepo,
        IDepartmentRepository departmentRepo,
        IOptions<JsonStorageOptions> storageOptions,
        ILogger<AttendanceReportService> logger,
        IAttendanceCalendarResolver? calendarResolver = null,
        ILeaveService? leaveService = null,
        ILeaveTypeRepository? leaveTypeRepo = null)
    {
        _attendanceRepo = attendanceRepo;
        _employeeRepo = employeeRepo;
        _departmentRepo = departmentRepo;
        _storageOptions = storageOptions.Value;
        _logger = logger;
        _calendarResolver = calendarResolver;
        _leaveService = leaveService;
        _leaveTypeRepo = leaveTypeRepo;
    }

    public async Task<MonthlyAttendanceReportViewModel> GenerateMonthlyReportAsync(AttendanceReportFilterModel filter, string preparedBy)
    {
        var now = DateTime.Now;
        if (filter.Month < 1 || filter.Month > 12) filter.Month = now.Month;
        if (filter.Year < 2000 || filter.Year > 2100) filter.Year = now.Year;

        int daysInMonth = DateTime.DaysInMonth(filter.Year, filter.Month);
        var validDays = Enumerable.Range(1, daysInMonth).ToList();

        var startDate = new DateOnly(filter.Year, filter.Month, 1);
        var endDate = new DateOnly(filter.Year, filter.Month, daysInMonth);
        var today = DateOnly.FromDateTime(now);

        var allEmployees = await _employeeRepo.GetAllAsync();
        var departments = await _departmentRepo.GetAllAsync();
        var deptDict = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        // Fetch monthly attendance records
        var monthlyAttendance = await _attendanceRepo.GetDateRangeAsync(startDate, endDate);
        var attendanceLookup = monthlyAttendance
            .GroupBy(a => (a.EmployeeId, a.AttendanceDate))
            .ToDictionary(g => g.Key, g => g.ToList());

        var allLeaveTypes = _leaveTypeRepo != null ? await _leaveTypeRepo.GetAllAsync() : new List<LeaveTypeModel>();

        var employeeViewModels = new List<EmployeeMonthlyAttendanceViewModel>();

        foreach (var emp in allEmployees.OrderBy(e => e.EmployeeNo))
        {
            // Department Filter
            if (!string.IsNullOrEmpty(filter.DepartmentId) && !string.Equals(emp.DepartmentId, filter.DepartmentId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Search Term Filter
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                bool matches = emp.EmployeeNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                               emp.FullName.Contains(term, StringComparison.OrdinalIgnoreCase);
                if (!matches) continue;
            }

            var deptName = !string.IsNullOrEmpty(emp.DepartmentId) && deptDict.TryGetValue(emp.DepartmentId, out var dn)
                ? dn
                : (emp.DepartmentName ?? "General");

            string designation = (deptName.Contains("Production") ? "Production Staff" :
                                  deptName.Contains("HR") ? "HR Associate" :
                                  deptName.Contains("Security") ? "Security Guard" : "Staff Member");

            var empReport = new EmployeeMonthlyAttendanceViewModel
            {
                EmployeeId = emp.EmployeeId,
                EmployeeNo = emp.EmployeeNo,
                EmployeeName = emp.FullName,
                Designation = designation,
                DepartmentName = deptName,
                PhotoPath = emp.Photo,
                JoiningDate = emp.JoiningDate
            };

            var approvedLeaves = _leaveService != null
                ? await _leaveService.GetApprovedLeavesForEmployeeAsync(emp.EmployeeId, startDate, endDate)
                : new List<LeaveApplicationModel>();

            int presentCount = 0;
            int absentCount = 0;
            int holidayCount = 0;

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateOnly(filter.Year, filter.Month, day);
                var dayOfWeek = date.DayOfWeek;
                bool isWeekend = (dayOfWeek == DayOfWeek.Sunday);

                var status = new DailyAttendanceStatusViewModel
                {
                    Day = day,
                    Date = date,
                    IsWeekend = isWeekend
                };

                if (_calendarResolver != null)
                {
                    attendanceLookup.TryGetValue((emp.EmployeeId, date), out var dayPunches);
                    var resolved = await _calendarResolver.ResolveDailyStatusAsync(
                        emp, date, dayPunches ?? new List<AttendanceModel>(), approvedLeaves, allLeaveTypes, today);

                    status.StatusCode = resolved.StatusCode;
                    status.StatusName = resolved.StatusName;
                    status.CssClass = resolved.CssClass;
                    status.TooltipText = resolved.TooltipText;
                    status.IsBeforeJoining = resolved.IsBeforeJoining;
                    status.IsFutureDate = resolved.IsFutureDate;

                    if (resolved.StatusCode == "P") presentCount++;
                    else if (resolved.StatusCode == "A") absentCount++;
                    else if (resolved.StatusCode == "H") holidayCount++;
                    else if (resolved.StatusCode == "U") absentCount++;
                }
                else
                {
                    // Fallback rule-based resolution
                    if (emp.JoiningDate != default && date < emp.JoiningDate)
                    {
                        status.StatusCode = "";
                        status.StatusName = "Before Joining Date";
                        status.CssClass = "code-blank";
                        status.TooltipText = $"{emp.FullName} - Joined on {emp.JoiningDate:dd MMM yyyy} (Before Joining)";
                        status.IsBeforeJoining = true;
                    }
                    else if (date > today)
                    {
                        status.StatusCode = "";
                        status.StatusName = "Future Date";
                        status.CssClass = "code-blank";
                        status.TooltipText = $"{date:dd MMM yyyy} (Future Date)";
                        status.IsFutureDate = true;
                    }
                    else
                    {
                        bool hasAttendance = attendanceLookup.TryGetValue((emp.EmployeeId, date), out var dayPunches);

                        if (hasAttendance && dayPunches != null && dayPunches.Count > 0)
                        {
                            var punchIn = dayPunches.FirstOrDefault(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase));
                            var punchOut = dayPunches.FirstOrDefault(p => string.Equals(p.PunchType, "OUT", StringComparison.OrdinalIgnoreCase));

                            status.StatusCode = "P";
                            status.StatusName = "Present";
                            status.CssClass = "code-p";
                            string inTime = punchIn != null ? punchIn.PunchTime.ToString("hh:mm tt") : "--";
                            string outTime = punchOut != null ? punchOut.PunchTime.ToString("hh:mm tt") : "--";
                            status.TooltipText = $"{emp.FullName} - Present on {date:dd MMM} (IN: {inTime}, OUT: {outTime})";
                            presentCount++;
                        }
                        else if (isWeekend)
                        {
                            status.StatusCode = "H";
                            status.StatusName = "Sunday Holiday";
                            status.CssClass = "code-h";
                            status.TooltipText = $"{date:dd MMM yyyy} - Sunday (Holiday)";
                            holidayCount++;
                        }
                        else
                        {
                            status.StatusCode = "A";
                            status.StatusName = "Absent";
                            status.CssClass = "code-a";
                            status.TooltipText = $"{emp.FullName} - Absent on {date:dd MMM yyyy}";
                            absentCount++;
                        }
                    }
                }

                empReport.DailyStatuses[day] = status;
            }

            empReport.PresentDays = presentCount;
            empReport.AbsentDays = absentCount;
            empReport.HolidayDays = holidayCount;

            // Attendance Status Filter: "All", "Present", "Absent", "Holiday"
            if (!string.IsNullOrEmpty(filter.StatusFilter) && !string.Equals(filter.StatusFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(filter.StatusFilter, "Present", StringComparison.OrdinalIgnoreCase) && empReport.PresentDays == 0) continue;
                if (string.Equals(filter.StatusFilter, "Absent", StringComparison.OrdinalIgnoreCase) && empReport.AbsentDays == 0) continue;
                if (string.Equals(filter.StatusFilter, "Holiday", StringComparison.OrdinalIgnoreCase) && empReport.HolidayDays == 0) continue;
            }

            employeeViewModels.Add(empReport);
        }

        var summary = new AttendanceMonthlySummaryViewModel
        {
            TotalEmployees = employeeViewModels.Count,
            TotalPresentDays = employeeViewModels.Sum(e => e.PresentDays),
            TotalAbsentDays = employeeViewModels.Sum(e => e.AbsentDays),
            TotalHolidayDays = employeeViewModels.Sum(e => e.HolidayDays),
            AverageAttendancePercentage = employeeViewModels.Count > 0
                ? Math.Round(employeeViewModels.Average(e => e.AttendancePercentage), 1)
                : 0.0
        };

        return new MonthlyAttendanceReportViewModel
        {
            Filter = filter,
            BusinessName = !string.IsNullOrWhiteSpace(_storageOptions.CompanyName) ? _storageOptions.CompanyName : "Apex Industrial Technologies",
            PreparedBy = !string.IsNullOrWhiteSpace(preparedBy) ? preparedBy : "HR Administrator",
            GeneratedDate = now,
            DaysInMonth = daysInMonth,
            ValidDays = validDays,
            Employees = employeeViewModels,
            Summary = summary,
            Departments = departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToList()
        };
    }
}
