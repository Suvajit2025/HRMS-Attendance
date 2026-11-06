using HRMS.Models;

namespace HRMS.ViewModels;

public class FaceRegistrationViewModel
{
    public IReadOnlyList<EmployeeRegistrationItem> Employees { get; set; } = new List<EmployeeRegistrationItem>();
    public int TotalEmployees { get; set; }
    public int EnrolledCount { get; set; }
    public int PendingCount => TotalEmployees - EnrolledCount;
    public string ModelInfo { get; set; } = string.Empty;
}

public class EmployeeRegistrationItem
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? PhotoPath { get; set; }
    public bool HasFaceTemplate { get; set; }
    public DateTime? EnrolledDate { get; set; }
}

public class FaceEnrollmentRequestDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
}

public class FaceEnrollmentResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
    public bool FaceDetected { get; set; }
}

