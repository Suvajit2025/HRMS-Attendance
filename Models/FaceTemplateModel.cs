namespace HRMS.Models;

public class FaceTemplateModel
{
    public string TemplateId { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = "ONNX-MobileFace-v1";
    public string EmbeddingBase64 { get; set; } = string.Empty;
    public int VectorDimension { get; set; } = 128;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

