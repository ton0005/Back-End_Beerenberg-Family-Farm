using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FarmManagement.Application.DTOs;

public class StaffDto
{
    // StaffNumber: Unique 7-digit identifier, required
    [Required]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Staff number must be 5 digits.")]
    public string StaffNumber { get; set; } = string.Empty;

    // First Name: 1–50 chars; letters, spaces, hyphens, apostrophes
    [Required]
    [MaxLength(50)]
    [RegularExpression(@"^[A-Za-z\s\-']+$", ErrorMessage = "First name contains invalid characters.")]
    public string FirstName { get; set; } = string.Empty;

    // Last Name: 1–50 chars; letters, spaces, hyphens, apostrophes
    [Required]
    [MaxLength(50)]
    [RegularExpression(@"^[A-Za-z\s\-']+$", ErrorMessage = "Last name contains invalid characters.")]
    public string LastName { get; set; } = string.Empty;

    // Email: must contain @, valid format
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    // Phone: optional, 10 digits starting with 0 when provided
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0.")]
    public string? Phone { get; set; }

    // Address: max ~100 chars, free text
    [MaxLength(100)]
    public string? Address { get; set; }

    // Contract type
    [Required]
    public string ContractType { get; set; } = string.Empty;

    // Role ID
    // Staff role name (job role). Front-end should send the job role name using the JSON field name `staffRole`.
    // The service will map it to an internal RoleId.
    [Required]
    [MaxLength(100)]
    [JsonPropertyName("staffRole")]
    public string? StaffRole { get; set; }

    // NOTE: Front-end should send `staffRole` (job role name). `StaffRoleId` is kept for
    // backward-compatibility but the service will resolve the id from the provided name.
    public int? StaffRoleId { get; set; }

    // AccessRole(s) to assign in ASP.NET Identity (e.g. ["Admin"] or ["User"]).
    // Required: at least one access role must be provided when creating a staff.
    [Required]
    [MinLength(1, ErrorMessage = "At least one AccessRole must be provided.")]
    public string[] AccessRole { get; set; } = Array.Empty<string>();

    // Whether to send a temporary password email when the account is created.
    [Required]
    public bool SendTempPasswordEmail { get; set; } = true;

    // Optional: DepartmentId
    public int? DepartmentId { get; set; }

    // Optional: DepartmentName - if provided and DepartmentId is not supplied, service will resolve the name to an ID
    [MaxLength(100)]
    public string? DepartmentName { get; set; }

    // HireDate is required for new staff
    [Required]
    [DataType(DataType.Date)]
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    // Note: TerminationDate is intended for edits/updates only. It will be ignored during Create operations.
    
    // Weekly available working hours (required)
    [Required]
    [Range(0, 168, ErrorMessage = "WeeklyAvailableHour must be between 0 and 168 hours")]
    public int WeeklyAvailableHour { get; set; }
}
