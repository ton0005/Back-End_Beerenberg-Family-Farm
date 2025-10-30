using FarmManagement.Core.Entities;
using FarmManagement.Application.Security;
using FarmManagement.Application.Repositories;
using FarmManagement.Application.Models;
using FarmManagement.Application.Services;
using Microsoft.AspNetCore.Identity;
using FarmManagement.Core.Entities.Identity;

namespace FarmManagement.Application.Services;

public class AuthService
{
    private readonly IAuthUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IIdentityService _identityService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthService(IAuthUserRepository users, IPasswordHasher hasher, IIdentityService identityService, UserManager<ApplicationUser> userManager)
    {
        _users = users;
        _hasher = hasher;
        _identityService = identityService;
        _userManager = userManager;
    }

    // Registration uses staffId
    public async Task<AuthResult?> RegisterAsync(int staffId, string username, string plainPassword, CancellationToken ct)
    {
        var (hash, salt) = _hasher.Hash(plainPassword);
        var user = new AuthUser(staffId, username, hash, salt, DateTime.UtcNow);

        // Create corresponding Identity user and link it
        var (identityId, error) = await _identityService.CreateUserAsync(username, plainPassword, staffId);
        if (!string.IsNullOrWhiteSpace(identityId)) 
        {
            user.LinkIdentityUser(identityId);
        }
        else if (!string.IsNullOrWhiteSpace(error))
        {
            // Consider logging the error or handling it appropriately
            throw new InvalidOperationException($"Failed to create Identity user: {error}");
        }

        await _users.AddAsync(user, ct);
        return new AuthResult(user.StaffId.ToString(), user.Username);
    }

    public async Task<AuthResult?> LoginAsync(string username, string plainPassword, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user == null || user.IsAccountLocked()) return null;

        var isValidPassword = _hasher.Verify(plainPassword, user.PasswordHash, user.PasswordSalt);
        if (isValidPassword)
        {
            user.ResetFailedAttempts();
            user.UpdateLastLogin();
            await _users.UpdateAsync(user, ct);
            return new AuthResult(user.StaffId.ToString(), user.Username);
        }
        else
        {
            user.IncrementFailedAttempts();
            await _users.UpdateAsync(user, ct);
            return null;
        }
    }

    // 🔹 Reset password only by staffNumber
    public async Task<bool> ResetPasswordByStaffNumberAsync(string staffNumber, string newPlainPassword, CancellationToken ct)
    {
        var user = await _users.GetByStaffNumberAsync(staffNumber, ct);
        if (user == null || !user.IsActive || user.Staff == null) return false;
        if (!PasswordValidator.IsValid(newPlainPassword, out var error))
            throw new ArgumentException(error, nameof(newPlainPassword));

        var (hash, salt) = _hasher.Hash(newPlainPassword);
        user.UpdatePassword(hash, salt);

        if (user.IsLocked)
        {
            user.UnlockAccount();
        }

        await _users.UpdateAsync(user, ct);

        // 🔹 CRITICAL FIX: Also update Identity user password if linked
        // Without this, Identity login will fail because Identity password remains old
        if (!string.IsNullOrWhiteSpace(user.IdentityUserId))
        {
            try
            {
                var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
                if (identityUser != null)
                {
                    // Remove current password and set new one
                    var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                    var resetResult = await _userManager.ResetPasswordAsync(identityUser, token, newPlainPassword);
                    
                    if (resetResult.Succeeded)
                    {
                        // Also clear any lockout on Identity user
                        await _userManager.SetLockoutEndDateAsync(identityUser, null);
                        await _userManager.ResetAccessFailedCountAsync(identityUser);
                    }
                    else
                    {
                        // Log error but don't fail the operation - legacy auth still works
                        Console.WriteLine($"Failed to update Identity password for user {user.IdentityUserId}: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail - legacy auth still works
                Console.WriteLine($"Exception updating Identity password: {ex.Message}");
            }
        }

        return true;
    }


    public async Task<bool> UnlockAccountAsync(string username, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user == null) return false;

        user.UnlockAccount();
        await _users.UpdateAsync(user, ct);
        return true;
    }

    public async Task<bool> IsAccountLockedAsync(string username, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(username.Trim(), ct);
        return user?.FailedLoginAttempts >= 5;
    }

    public async Task<AuthUser?> GetUserByStaffNumberAsync(string staffNumber, CancellationToken ct)
    {
        return await _users.GetByStaffNumberAsync(staffNumber.Trim(), ct);
    }
}