using System.ComponentModel.DataAnnotations;
namespace FarmManagement.Application.DTOs;

public class PasswordResetRequestDto
{
    [Required]
    public string StaffNumber { get; set; } = string.Empty;
}