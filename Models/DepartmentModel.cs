namespace HRMS.Models;

public class DepartmentModel
{
    public string DepartmentId { get; set; } = Guid.NewGuid().ToString();
    public string DepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

