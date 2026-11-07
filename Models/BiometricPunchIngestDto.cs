namespace HRMS.Models;

public class BiometricPunchIngestDto
{
    // Identifier provided by biometric terminal (can be EmployeeNo e.g. EMP001 or numeric EnrollmentId e.g. 101 or CardNo)
    public string EmployeeNo { get; set; } = string.Empty;
    public string? CardNo { get; set; }
    public string? DeviceId { get; set; } = "EXTERNAL-TERMINAL";
    public string? DeviceName { get; set; }
    public DateTime? PunchTime { get; set; }
    public string? PunchType { get; set; } = "Auto"; // IN, OUT, CheckIn, CheckOut, Auto, or 0/1
    public string? VerificationMode { get; set; } = "FaceRecognition"; // FaceRecognition, Fingerprint, Card, Password
    public string? DeviceLocation { get; set; }
    public string? DeviceIp { get; set; }
    public string? PhotoBase64 { get; set; }
}

public class BiometricBulkSyncRequestDto
{
    public string? DeviceId { get; set; }
    public string? DeviceApiKey { get; set; }
    public List<BiometricPunchIngestDto> Punches { get; set; } = new();
}

public class BiometricSyncResultDto
{
    public bool Success { get; set; } = true;
    public int TotalReceived { get; set; }
    public int TotalProcessed { get; set; }
    public int TotalDuplicatesSkipped { get; set; }
    public int TotalErrors { get; set; }
    public List<string> Messages { get; set; } = new();
}

