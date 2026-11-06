namespace HRMS.Models;

public class EmployeeLeaveBalanceModel
{
    public string LeaveBalanceId { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string LeaveTypeId { get; set; } = string.Empty;
    public string? LeavePolicyId { get; set; }
    
    public double OpeningBalance { get; set; } = 0.0;
    public double Accrued { get; set; } = 0.0;
    public double Used { get; set; } = 0.0;
    public double Reserved { get; set; } = 0.0; // Approved for future dates
    public double Adjusted { get; set; } = 0.0;
    
    // Available Balance = Opening Balance + Accrued + Adjusted - Used - Reserved
    public double AvailableBalance => Math.Round(OpeningBalance + Accrued + Adjusted - Used - Reserved, 2);
    
    public int FinancialYear { get; set; } = DateTime.Today.Year;
    public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;
}

