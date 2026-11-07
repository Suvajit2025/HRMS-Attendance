using HRMS.Models;

namespace HRMS.ViewModels;

public class AttendanceReportFilterModel
{
    public int Month { get; set; } = DateTime.Today.Month;
    public int Year { get; set; } = DateTime.Today.Year;
    public string? DepartmentId { get; set; }
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; } = "All"; // "All", "Present", "Absent", "Holiday"
}

public class DailyAttendanceStatusViewModel
{
    public int Day { get; set; }
    public DateOnly Date { get; set; }
    public string StatusCode { get; set; } = ""; // P (Present), A (Absent), H (Holiday), or "" (Blank / Before Joining / Future)
    public string StatusName { get; set; } = "";
    public string CssClass { get; set; } = "";
    public string? TooltipText { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsBeforeJoining { get; set; }
    public bool IsFutureDate { get; set; }
}

public class EmployeeMonthlyAttendanceViewModel
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Designation { get; set; } = "Staff Member";
    public string DepartmentName { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public DateOnly JoiningDate { get; set; }

    public Dictionary<int, DailyAttendanceStatusViewModel> DailyStatuses { get; set; } = new();

    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int HolidayDays { get; set; }

    // Backward-compatible properties
    public int AttendedDays
    {
        get => PresentDays;
        set => PresentDays = value;
    }
    public int SickPtoDays { get; set; } = 0;
    public int UnpaidLeaveDays { get; set; } = 0;
    public int NoShowDays
    {
        get => AbsentDays;
        set => AbsentDays = value;
    }

    public int TotalMarkedDays => PresentDays + AbsentDays + HolidayDays;

    public double AttendancePercentage => TotalMarkedDays > 0
        ? Math.Round((PresentDays / (double)TotalMarkedDays) * 100.0, 1)
        : 0.0;
}

public class AttendanceMonthlySummaryViewModel
{
    public int TotalEmployees { get; set; }
    public int TotalPresentDays { get; set; }
    public int TotalAbsentDays { get; set; }
    public int TotalHolidayDays { get; set; }

    public int TotalSickPtoDays { get; set; } = 0;
    public int TotalUnpaidLeaveDays { get; set; } = 0;
    public int TotalNoShowDays
    {
        get => TotalAbsentDays;
        set => TotalAbsentDays = value;
    }

    public double AverageAttendancePercentage { get; set; }
}

public class MonthlyAttendanceReportViewModel
{
    public AttendanceReportFilterModel Filter { get; set; } = new();
    public string BusinessName { get; set; } = "Demo Technologies";
    public string PreparedBy { get; set; } = "HR Administrator";
    public DateTime GeneratedDate { get; set; } = DateTime.Now;
    public int DaysInMonth { get; set; } = 31;
    public List<int> ValidDays { get; set; } = new();

    public List<EmployeeMonthlyAttendanceViewModel> Employees { get; set; } = new();
    public AttendanceMonthlySummaryViewModel Summary { get; set; } = new();
    public IReadOnlyList<DepartmentModel> Departments { get; set; } = new List<DepartmentModel>();

    public string MonthName => new DateTime(Filter.Year, Filter.Month, 1).ToString("MMMM yyyy");
}
