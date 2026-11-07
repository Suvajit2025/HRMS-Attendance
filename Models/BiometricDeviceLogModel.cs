namespace HRMS.Models;

public class BiometricDeviceLogModel
{
    public string LogId { get; set; } = Guid.NewGuid().ToString();
    public string DeviceId { get; set; } = "EXTERNAL-TERMINAL";
    public string? DeviceName { get; set; }
    public string RawEmployeeIdentifier { get; set; } = string.Empty;
    public string? ResolvedEmployeeId { get; set; }
    public string? ResolvedEmployeeNo { get; set; }
    public string? ResolvedEmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime DevicePunchTime { get; set; } = DateTime.UtcNow;
    public string DevicePunchType { get; set; } = "Auto"; // IN, OUT, CheckIn, CheckOut, Auto
    public string ResolvedPunchType { get; set; } = "IN"; // IN or OUT
    public string VerificationMode { get; set; } = "FaceRecognition"; // FaceRecognition, Fingerprint, Card, Password
    public string SourceType { get; set; } = "API"; // API, DatabaseSync, ManualSync, Simulator
    public string IngestionStatus { get; set; } = "Success"; // Success, DuplicateSkipped, EmployeeNotFound, Error
    public string? Remarks { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}

