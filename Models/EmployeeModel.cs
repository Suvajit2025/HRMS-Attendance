using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRMS.Models;

public class EmployeeModel
{
    public string EmployeeId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(20)]
    public string EmployeeNo { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? MiddleName { get; set; }

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateOnly DOB { get; set; }

    [Phone]
    [StringLength(20)]
    public string? PhoneNo { get; set; }

    [Required]
    public DateOnly JoiningDate { get; set; }

    [Required]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar number must be exactly 12 numeric digits.")]
    public string AadhaarNo { get; set; } = string.Empty;

    [StringLength(20)]
    public string Gender { get; set; } = "Male";

    public string? Photo { get; set; }

    public string? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    [JsonIgnore]
    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}".Trim()
        : $"{FirstName} {MiddleName} {LastName}".Trim();

    [JsonIgnore]
    public string MaskedAadhaar
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AadhaarNo) || AadhaarNo.Length < 4)
                return "•••• •••• ••••";

            var clean = AadhaarNo.Trim();
            if (clean.Length >= 12)
                return $"•••• •••• {clean[^4..]}";

            return $"•••• •••• {clean[^4..]}";
        }
    }
}

