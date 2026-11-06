namespace HRMS.Models;

public class LeavePolicyModel
{
    public string LeavePolicyId { get; set; } = Guid.NewGuid().ToString();
    public string PolicyName { get; set; } = string.Empty;
    public string LeaveTypeId { get; set; } = string.Empty;
    public string? DepartmentId { get; set; } // Optional department scope
    
    public double AnnualEntitlement { get; set; } = 12.0;
    public string AccrualFrequency { get; set; } = "Annual"; // Annual, Monthly, Quarterly
    public double AccrualAmount { get; set; } = 1.0;
    public double MaximumBalance { get; set; } = 30.0;
    
    public bool CarryForwardAllowed { get; set; } = false;
    public double MaximumCarryForward { get; set; } = 0.0;
    
    public int MinimumNoticeDays { get; set; } = 0;
    public int? MaximumConsecutiveDays { get; set; }
    public bool AllowNegativeBalance { get; set; } = false;
    
    // Calendar calculation rules
    public bool ExcludeWeeklyOffs { get; set; } = true;
    public bool ExcludeHolidays { get; set; } = true;
    public bool EnableSandwichRule { get; set; } = false; // Disabled by default
    
    public DateOnly ApplicableFrom { get; set; } = new DateOnly(2020, 1, 1);
    public DateOnly? ApplicableTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

