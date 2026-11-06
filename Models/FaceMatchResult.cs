namespace HRMS.Models;

public class FaceMatchResult
{
    public bool IsSuccess { get; set; }
    public bool FaceDetected { get; set; }
    public string? EmployeeId { get; set; }
    public string? EmployeeNo { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public string? PhotoPath { get; set; }
    public float Confidence { get; set; }
    public string Message { get; set; } = string.Empty;
}

