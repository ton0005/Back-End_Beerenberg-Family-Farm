using System.ComponentModel.DataAnnotations;
namespace FarmManagement.Application.DTOs;

public class PasswordResetDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}