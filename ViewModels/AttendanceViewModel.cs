using HRMS.Models;

namespace HRMS.ViewModels;

public class DailyAttendanceViewModel
{
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? DepartmentId { get; set; }
    public string? SearchTerm { get; set; }
    public IReadOnlyList<DepartmentModel> Departments { get; set; } = new List<DepartmentModel>();
    public List<DailyAttendanceRow> Records { get; set; } = new();

    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int InsideCount { get; set; }
    public int LateCount { get; set; }
}

public class AttendanceHistoryViewModel
{
    public string? SelectedEmployeeId { get; set; }
    public string? SelectedEmployeeNo { get; set; }
    public string? SelectedEmployeeName { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public IReadOnlyList<EmployeeModel> Employees { get; set; } = new List<EmployeeModel>();
    public IReadOnlyList<AttendanceModel> Records { get; set; } = new List<AttendanceModel>();
}

public class AttendanceTerminalViewModel
{
    public string CompanyName { get; set; } = "Demo Technologies";
    public string ShiftStartTime { get; set; } = "09:00:00";
    public float FaceMatchThreshold { get; set; } = 0.65f;
    public int TodayPunchesCount { get; set; }
    public IReadOnlyList<AttendanceActivityItem> RecentTerminalActivity { get; set; } = new List<AttendanceActivityItem>();
    public IReadOnlyList<EmployeeModel> RegisteredEmployees { get; set; } = new List<EmployeeModel>();
}

public class PunchRequestDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string PunchType { get; set; } = "IN"; // "IN" or "OUT"
    public string VerificationMethod { get; set; } = "Manual";
    public string? CapturedPhotoBase64 { get; set; }
}

public class FacePunchRequestDto
{
    public string ImageBase64 { get; set; } = string.Empty;
    public string PunchType { get; set; } = "IN"; // "IN" or "OUT"
}

public class PunchResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PunchType { get; set; }
    public string? PunchTimeFormatted { get; set; }
    public string? EmployeeNo { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public string? PhotoPath { get; set; }
    public float? MatchScore { get; set; }
    public bool IsLate { get; set; }
}

