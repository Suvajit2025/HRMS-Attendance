using System.ComponentModel.DataAnnotations;
using HRMS.Models;
using Microsoft.AspNetCore.Http;

namespace HRMS.ViewModels;

public class EmployeeListViewModel
{
    public IReadOnlyList<EmployeeModel> Employees { get; set; } = new List<EmployeeModel>();
    public string? SearchTerm { get; set; }
    public string? DepartmentId { get; set; }
    public string? StatusFilter { get; set; } // "all", "active", "inactive"
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
    public IReadOnlyList<DepartmentModel> Departments { get; set; } = new List<DepartmentModel>();
}

public class EmployeeEditViewModel
{
    public string? EmployeeId { get; set; }

    [Required(ErrorMessage = "Employee Number is required")]
    [Display(Name = "Employee Number")]
    [StringLength(20)]
    public string EmployeeNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "First Name is required")]
    [Display(Name = "First Name")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Middle Name")]
    [StringLength(50)]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Last Name is required")]
    [Display(Name = "Last Name")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of Birth is required")]
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateOnly DOB { get; set; } = new DateOnly(1995, 1, 1);

    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [Display(Name = "Phone Number")]
    [StringLength(20)]
    public string? PhoneNo { get; set; }

    [Required(ErrorMessage = "Joining Date is required")]
    [Display(Name = "Joining Date")]
    [DataType(DataType.Date)]
    public DateOnly JoiningDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Aadhaar Number is required")]
    [Display(Name = "Aadhaar Number (12 digits)")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar number must contain exactly 12 numeric digits")]
    public string AadhaarNo { get; set; } = string.Empty;

    [Display(Name = "Gender")]
    public string Gender { get; set; } = "Male";

    [Display(Name = "Department")]
    public string? DepartmentId { get; set; }

    [Display(Name = "Active Status")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Upload Photograph")]
    public IFormFile? PhotoFile { get; set; }

    public string? CameraPhotoBase64 { get; set; }
    public string? ExistingPhotoPath { get; set; }

    public IReadOnlyList<DepartmentModel> Departments { get; set; } = new List<DepartmentModel>();

    public bool IsNew => string.IsNullOrEmpty(EmployeeId);
}

public class EmployeeDetailsViewModel
{
    public EmployeeModel Employee { get; set; } = new();
    public string? DepartmentName { get; set; }
    public bool HasFaceRegistered { get; set; }
    public IReadOnlyList<AttendanceModel> RecentAttendance { get; set; } = new List<AttendanceModel>();
}

