namespace HRMS.Models;

public class WeeklyOffModel
{
    public string WeeklyOffId { get; set; } = Guid.NewGuid().ToString();
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;
    public bool IsWeeklyOff { get; set; } = true;
    
    // Optional Alternate Saturday support (e.g. 2nd and 4th Saturday)
    public bool IsAlternateWeek { get; set; } = false;
    public int? AlternateWeekNumber { get; set; } // 1, 2, 3, 4, 5
    
    // Scoping
    public string? LocationCode { get; set; }
    public string? DepartmentId { get; set; }
    
    public DateOnly EffectiveFrom { get; set; } = new DateOnly(2020, 1, 1);
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

