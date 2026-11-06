using HRMS.Models;

namespace HRMS.ViewModels;

public class HolidayCalendarViewModel
{
    public int Year { get; set; } = DateTime.Today.Year;
    public int Month { get; set; } = DateTime.Today.Month;
    public string? SelectedDepartmentId { get; set; }
    public string? SelectedLocationCode { get; set; }
    public string? SelectedHolidayType { get; set; }
    
    public IReadOnlyList<DepartmentModel> Departments { get; set; } = new List<DepartmentModel>();
    public IReadOnlyList<HolidayModel> Holidays { get; set; } = new List<HolidayModel>();
    public IReadOnlyList<HolidayModel> UpcomingHolidays { get; set; } = new List<HolidayModel>();
    public IReadOnlyList<WeeklyOffModel> WeeklyOffs { get; set; } = new List<WeeklyOffModel>();
    
    // Calendar matrix
    public int DaysInMonth => DateTime.DaysInMonth(Year, Month);
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
}

