using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.Extensions.Options;

namespace HRMS.Repositories;

public class JsonDashboardRepository : IDashboardRepository
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly JsonStorageOptions _options;

    public JsonDashboardRepository(
        IEmployeeRepository employeeRepo,
        IAttendanceRepository attendanceRepo,
        IDepartmentRepository departmentRepo,
        IOptions<JsonStorageOptions> options)
    {
        _employeeRepo = employeeRepo;
        _attendanceRepo = attendanceRepo;
        _departmentRepo = departmentRepo;
        _options = options.Value;
    }

    public async Task<DashboardModel> GetDashboardStatsAsync(DateOnly date, string? departmentId = null, string? searchTerm = null)
    {
        var allEmployees = await _employeeRepo.GetAllAsync();
        var activeEmployees = allEmployees.Where(e => e.IsActive).ToList();
        var departments = await _departmentRepo.GetAllAsync();
        var deptDict = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

        // Filter employees if department or search term is applied
        var filteredEmployees = activeEmployees.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            filteredEmployees = filteredEmployees.Where(e => string.Equals(e.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            filteredEmployees = filteredEmployees.Where(e =>
                e.EmployeeNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.FullName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var empList = filteredEmployees.ToList();

        // Get all attendance for the given date
        var attendanceForDate = await _attendanceRepo.GetByDateAsync(date);

        // Group punches by employee
        var punchesByEmp = attendanceForDate
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.PunchTime).ToList());

        // Parse shift start time
        if (!TimeOnly.TryParse(_options.ShiftStartTime, out var shiftStart))
        {
            shiftStart = new TimeOnly(9, 0, 0);
        }

        int presentCount = 0;
        int currentlyInsideCount = 0;
        int lateArrivalCount = 0;

        var todayRows = new List<DailyAttendanceRow>();

        foreach (var emp in empList)
        {
            var hasPunches = punchesByEmp.TryGetValue(emp.EmployeeId, out var empPunches);
            var firstIn = empPunches?.FirstOrDefault(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase));
            var lastOut = empPunches?.LastOrDefault(p => string.Equals(p.PunchType, "OUT", StringComparison.OrdinalIgnoreCase));

            bool isPresent = firstIn != null;
            bool isCurrentlyInside = false;
            bool isLate = false;
            string status = "Absent";
            string badgeClass = "bg-danger";
            string workingHours = "--:--";

            if (isPresent)
            {
                presentCount++;

                var punchTimeOnly = TimeOnly.FromDateTime(firstIn!.PunchTime);
                if (punchTimeOnly > shiftStart)
                {
                    isLate = true;
                    lateArrivalCount++;
                }

                // Check if currently inside (last punch of day was IN, or no OUT yet)
                var lastPunch = empPunches!.Last();
                if (string.Equals(lastPunch.PunchType, "IN", StringComparison.OrdinalIgnoreCase))
                {
                    isCurrentlyInside = true;
                    currentlyInsideCount++;
                    status = isLate ? "Late (Inside)" : "Currently Inside";
                    badgeClass = isLate ? "bg-warning text-dark" : "bg-primary";
                }
                else
                {
                    status = isLate ? "Late (Punched Out)" : "Completed";
                    badgeClass = isLate ? "bg-warning text-dark" : "bg-success";
                }

                if (lastOut != null && lastOut.PunchTime > firstIn.PunchTime)
                {
                    var span = lastOut.PunchTime - firstIn.PunchTime;
                    workingHours = $"{(int)span.TotalHours:D2}h {span.Minutes:D2}m";
                }
                else if (isCurrentlyInside)
                {
                    var span = DateTime.UtcNow - firstIn.PunchTime;
                    if (span.TotalMinutes > 0)
                    {
                        workingHours = $"{(int)span.TotalHours:D2}h {span.Minutes:D2}m (in prog)";
                    }
                }
            }

            var deptName = !string.IsNullOrEmpty(emp.DepartmentId) && deptDict.TryGetValue(emp.DepartmentId, out var dn)
                ? dn
                : (emp.DepartmentName ?? "Unassigned");

            todayRows.Add(new DailyAttendanceRow
            {
                EmployeeId = emp.EmployeeId,
                EmployeeNo = emp.EmployeeNo,
                EmployeeName = emp.FullName,
                DepartmentName = deptName,
                PhotoPath = emp.Photo,
                FirstInTime = firstIn?.PunchTime,
                LastOutTime = lastOut?.PunchTime,
                WorkingHoursFormatted = workingHours,
                AttendanceStatus = status,
                StatusBadgeClass = badgeClass
            });
        }

        int totalEmp = empList.Count;
        int absentCount = Math.Max(0, totalEmp - presentCount);
        double attendancePercent = totalEmp > 0 ? Math.Round(((double)presentCount / totalEmp) * 100.0, 1) : 0.0;

        // Recent punch activities (latest 8 punches)
        var recentActivities = attendanceForDate
            .OrderByDescending(a => a.PunchTime)
            .Take(8)
            .Select(a =>
            {
                var emp = allEmployees.FirstOrDefault(e => e.EmployeeId == a.EmployeeId);
                return new AttendanceActivityItem
                {
                    AttendanceId = a.AttendanceId,
                    EmployeeNo = a.EmployeeNo ?? emp?.EmployeeNo ?? "EMP",
                    EmployeeName = a.EmployeeName ?? emp?.FullName ?? "Employee",
                    DepartmentName = a.DepartmentName ?? emp?.DepartmentName,
                    PhotoPath = a.CapturedPhotoPath ?? emp?.Photo,
                    PunchType = a.PunchType,
                    PunchTime = a.PunchTime,
                    VerificationMethod = a.VerificationMethod,
                    MatchScore = a.MatchScore,
                    IsLate = a.IsLate
                };
            })
            .ToList();

        // Department-wise attendance stats
        var deptStats = new List<DepartmentAttendanceStat>();
        foreach (var dept in departments)
        {
            var deptEmployees = activeEmployees.Where(e => string.Equals(e.DepartmentId, dept.DepartmentId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (deptEmployees.Count > 0)
            {
                int deptPresent = deptEmployees.Count(e => punchesByEmp.ContainsKey(e.EmployeeId) && punchesByEmp[e.EmployeeId].Any(p => p.PunchType == "IN"));
                deptStats.Add(new DepartmentAttendanceStat
                {
                    DepartmentName = dept.DepartmentName,
                    TotalCount = deptEmployees.Count,
                    PresentCount = deptPresent,
                    AbsentCount = Math.Max(0, deptEmployees.Count - deptPresent)
                });
            }
        }

        // 7-day trend
        var trends = new List<DailyAttendanceTrend>();
        for (int i = 6; i >= 0; i--)
        {
            var trendDate = date.AddDays(-i);
            var trendAttendance = await _attendanceRepo.GetByDateAsync(trendDate);
            var presentEmpIds = trendAttendance
                .Where(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.EmployeeId)
                .Distinct()
                .Count();

            var lateEmpIds = trendAttendance
                .Where(p => string.Equals(p.PunchType, "IN", StringComparison.OrdinalIgnoreCase) && p.IsLate)
                .Select(p => p.EmployeeId)
                .Distinct()
                .Count();

            trends.Add(new DailyAttendanceTrend
            {
                Date = trendDate,
                DateFormatted = trendDate.ToString("ddd, dd MMM"),
                PresentCount = presentEmpIds,
                AbsentCount = Math.Max(0, activeEmployees.Count - presentEmpIds),
                LateCount = lateEmpIds
            });
        }

        return new DashboardModel
        {
            TotalEmployees = totalEmp,
            PresentToday = presentCount,
            AbsentToday = absentCount,
            CurrentlyInside = currentlyInsideCount,
            LateArrivals = lateArrivalCount,
            AttendancePercentage = attendancePercent,
            SelectedDate = date,
            SelectedDepartmentId = departmentId,
            SearchTerm = searchTerm,
            RecentActivities = recentActivities,
            TodayAttendanceRecords = todayRows.OrderBy(r => r.AttendanceStatus == "Absent" ? 1 : 0).ThenBy(r => r.EmployeeNo).ToList(),
            DepartmentStats = deptStats,
            Last7DaysTrends = trends
        };
    }
}

