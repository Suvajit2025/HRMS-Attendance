namespace HRMS.Models;

public class AppSettingsModel
{
    public string CompanyName { get; set; } = "Demo Technologies";
    public string AppTitle { get; set; } = "HRMS — Workforce Management";
    public string ShiftStartTime { get; set; } = "09:00:00";
    public string ShiftEndTime { get; set; } = "18:00:00";
    public string TimeZoneId { get; set; } = "India Standard Time";
    public float FaceMatchThreshold { get; set; } = 0.65f;
    public string AdminUsername { get; set; } = "admin";
    public string AdminPasswordHash { get; set; } = "AQAAAAIAAYagAAAAEKv33cZ1z..."; // demo credential hash
}

