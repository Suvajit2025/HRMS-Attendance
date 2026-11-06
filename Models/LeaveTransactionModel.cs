namespace HRMS.Models;

public class LeaveTransactionModel
{
    public string LeaveTransactionId { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string LeaveTypeId { get; set; } = string.Empty;
    public string? LeaveApplicationId { get; set; }
    
    // TransactionType: OpeningBalance, Accrual, LeaveReserved, LeaveConsumed, LeaveReleased, Adjustment, CarryForward, Encashment
    public string TransactionType { get; set; } = "Accrual";
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public double Quantity { get; set; } = 0.0;
    public string Reference { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "System";
}

