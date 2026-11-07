namespace HRMS.Data;

public class JsonStorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Json";
    public string? FolderPath { get; set; }
    public string? DataDirectory { get; set; }
    public string? UploadsDirectory { get; set; }
    public string? ConnectionString { get; set; }
    public string ShiftStartTime { get; set; } = "09:00:00";
    public string ShiftEndTime { get; set; } = "18:00:00";
    public string TimeZoneId { get; set; } = "India Standard Time";
    public float FaceMatchThreshold { get; set; } = 0.65f;
    public string CompanyName { get; set; } = "Demo Technologies";
    public string AppTitle { get; set; } = "HRMS — Workforce Management";
}
