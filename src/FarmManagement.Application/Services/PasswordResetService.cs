using System;
using System.Security.Cryptography;
using FarmManagement.Core.Entities;
using FarmManagement.Application.Repositories;

namespace FarmManagement.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IAuthUserRepository _authUserRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly AuthService _authService;
    private readonly IEmailService _emailService;

    public PasswordResetService(
        IAuthUserRepository authUserRepository,
        IPasswordResetTokenRepository tokenRepository,
        AuthService authService,
        IEmailService emailService)
    {
        _authUserRepository = authUserRepository;
        _tokenRepository = tokenRepository;
        _authService = authService;
        _emailService = emailService;
    }

    // Step 1: Generate a secure password reset token and send email
    public async Task<bool> SendResetTokenByStaffNumberAsync(string staffNumber, CancellationToken ct = default)
    {
        var user = await _authUserRepository.GetByStaffNumberAsync(staffNumber, ct);
        if (user == null || !user.IsActive || user.Staff == null)
            return false;

        var token = GenerateSecureToken();

        var resetToken = new PasswordResetToken
        {
            StaffNumber = staffNumber, // store staffNumber instead of StaffId
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _tokenRepository.AddAsync(resetToken, ct);

        // Send token to user's email. Treat email failures as non-fatal: the token
        // is persisted and can be used to reset the password even if the email
        // provider rejects server-side API calls (for example EmailJS 403).
        try
        {
            var emailSent = await _emailService.SendPasswordResetEmailAsync(user.Username, token);
            // If email failed, still return true because token was created.
            return true;
        }
        catch
        {
            // Best-effort: swallow email exceptions so callers know token exists.
            return true;
        }
    }

    // Step 2: Validate the reset token
    public async Task<AuthUser?> ValidateResetTokenAsync(string token, CancellationToken ct = default)
    {
        var resetToken = await _tokenRepository.GetByTokenAsync(token, ct);
        if (resetToken == null || !resetToken.IsValid) return null;

        var user = await _authUserRepository.GetByStaffNumberAsync(resetToken.StaffNumber!, ct);
        if (user == null || user.Staff == null) return null;

        return user;
    }

    // Step 3: Reset the password using the token
    public async Task<bool> ResetPasswordWithTokenAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var resetToken = await _tokenRepository.GetByTokenAsync(token, ct);
        if (resetToken == null || !resetToken.IsValid) return false;

        var user = await _authUserRepository.GetByStaffNumberAsync(resetToken.StaffNumber!, ct);
        if (user == null || user.Staff == null) return false;

        // Use staffNumber-based reset in AuthService
        var success = await _authService.ResetPasswordByStaffNumberAsync(user.Staff.StaffNumber, newPassword, ct);

        if (success)
        {
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;
            await _tokenRepository.UpdateAsync(resetToken, ct);
        }

        return success;
    }

    // Helper: generate a cryptographically secure token
    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
