namespace HRMS.Models;

public class DashboardModel
{
    public int TotalEmployees { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int CurrentlyInside { get; set; }
    public int LateArrivals { get; set; }
    public double AttendancePercentage { get; set; }

    public DateOnly SelectedDate { get; set; }
    public string? SelectedDepartmentId { get; set; }
    public string? SearchTerm { get; set; }

    public List<AttendanceActivityItem> RecentActivities { get; set; } = new();
    public List<DailyAttendanceRow> TodayAttendanceRecords { get; set; } = new();
    public List<DepartmentAttendanceStat> DepartmentStats { get; set; } = new();
    public List<DailyAttendanceTrend> Last7DaysTrends { get; set; } = new();
}

public class AttendanceActivityItem
{
    public string AttendanceId { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? PhotoPath { get; set; }
    public string PunchType { get; set; } = "IN"; // IN or OUT
    public DateTime PunchTime { get; set; }
    public string VerificationMethod { get; set; } = "FaceRecognition";
    public float? MatchScore { get; set; }
    public bool IsLate { get; set; }
}

public class DailyAttendanceRow
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime? FirstInTime { get; set; }
    public DateTime? LastOutTime { get; set; }
    public string WorkingHoursFormatted { get; set; } = "--:--";
    public string AttendanceStatus { get; set; } = "Absent"; // Present, Late, Currently Inside, Completed, Absent
    public string StatusBadgeClass { get; set; } = "bg-secondary";
}

public class DepartmentAttendanceStat
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
}

public class DailyAttendanceTrend
{
    public string DateFormatted { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
}

