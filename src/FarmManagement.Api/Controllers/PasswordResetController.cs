using Microsoft.AspNetCore.Mvc;
using FarmManagement.Application.Services;
using FarmManagement.Application.Repositories;
using FarmManagement.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;
    
    public PasswordResetController(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    /// <summary>
    /// Request a password reset token to be sent via email
    /// </summary>
    [HttpPost("request-token")]
    public async Task<IActionResult> RequestToken([FromBody] PasswordResetRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var success = await _passwordResetService.SendResetTokenByStaffNumberAsync(request.StaffNumber, ct);
            
            if (!success)
            {
                return BadRequest(new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Staff member not found or account is inactive"
                });
            }

            return Ok(new PasswordResetResponseDto
            {
                Success = true,
                Message = "Password reset email sent successfully"
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new PasswordResetResponseDto
            {
                Success = false,
                Message = "An error occurred while sending the reset email"
            });
        }
    }

    /// <summary>
    /// Reset password using token
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var success = await _passwordResetService.ResetPasswordWithTokenAsync(request.Token, request.NewPassword, ct);
            
            if (!success)
            {
                return BadRequest(new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired token"
                });
            }

            return Ok(new PasswordResetResponseDto
            {
                Success = true,
                Message = "Password reset successfully"
            });
        }
        catch (ArgumentException ex)
        {
            // This handles password validation errors from PasswordValidator
            return BadRequest(new PasswordResetResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new PasswordResetResponseDto
            {
                Success = false,
                Message = "An error occurred while resetting the password"
            });
        }
    }

    /// <summary>
    /// Validate reset token
    /// </summary>
    [HttpGet("validate/{token}")]
    public async Task<IActionResult> ValidateToken(string token, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Token is required"
                });
            }

            var user = await _passwordResetService.ValidateResetTokenAsync(token, ct);
            
            if (user == null)
            {
                return BadRequest(new PasswordResetResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired token"
                });
            }

            return Ok(new PasswordResetResponseDto
            {
                Success = true,
                Message = "Token is valid"
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new PasswordResetResponseDto
            {
                Success = false,
                Message = "An error occurred while validating the token"
            });
        }
    }
}