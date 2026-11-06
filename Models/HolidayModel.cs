namespace HRMS.Models;

public class HolidayModel
{
    public string HolidayId { get; set; } = Guid.NewGuid().ToString();
    public string HolidayName { get; set; } = string.Empty;
    public DateOnly HolidayDate { get; set; }
    
    // HolidayType: Public, National, Festival, Optional, Company
    public string HolidayType { get; set; } = "Public";
    public string? Description { get; set; }
    public bool IsPaidHoliday { get; set; } = true;
    public bool IsActive { get; set; } = true;
    
    // Optional Scope: if both null/empty, applies to entire organization
    public string? LocationCode { get; set; }
    public string? DepartmentId { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

