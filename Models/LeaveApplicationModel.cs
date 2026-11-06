namespace HRMS.Models;

public class LeaveApplicationModel
{
    public string LeaveApplicationId { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string LeaveTypeId { get; set; } = string.Empty;
    
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public double TotalDays { get; set; } = 1.0;
    
    public bool IsHalfDay { get; set; } = false;
    public string? HalfDaySession { get; set; } // "FirstHalf" or "SecondHalf"
    
    public string Reason { get; set; } = string.Empty;
    public string? SupportingDocumentPath { get; set; }
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    
    // Statuses: Draft, Pending, Approved, Rejected, Cancelled, Cancellation Pending
    public string Status { get; set; } = "Pending";
    
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
}

