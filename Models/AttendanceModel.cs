namespace HRMS.Models;

public class AttendanceModel
{
    public string AttendanceId { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string? EmployeeNo { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public string PunchType { get; set; } = "IN"; // "IN" or "OUT"
    public DateTime PunchTime { get; set; } = DateTime.UtcNow;
    public string? CapturedPhotoPath { get; set; }
    public float? MatchScore { get; set; }
    public string VerificationMethod { get; set; } = "FaceRecognition"; // "FaceRecognition" or "Manual"
    public bool IsLate { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

