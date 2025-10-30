using System.ComponentModel.DataAnnotations;
namespace FarmManagement.Application.DTOs;

public class PasswordResetResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}