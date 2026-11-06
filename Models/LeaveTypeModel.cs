namespace HRMS.Models;

public class LeaveTypeModel
{
    public string LeaveTypeId { get; set; } = Guid.NewGuid().ToString();
    public string LeaveCode { get; set; } = string.Empty; // CL, SL, EL, PL, LWP, CO
    public string LeaveName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPaidLeave { get; set; } = true;
    public bool IsHalfDayAllowed { get; set; } = true;
    public bool IsCarryForwardAllowed { get; set; } = false;
    public int MaximumCarryForward { get; set; } = 0;
    public bool RequiresApproval { get; set; } = true;
    public bool RequiresDocument { get; set; } = false;
    public int? MaximumConsecutiveDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

