using FarmManagement.Core.Entities;

namespace FarmManagement.Application.Repositories;

public interface IPasswordResetService
{
    Task<bool> SendResetTokenByStaffNumberAsync(string staffNumber, CancellationToken ct);
    Task<AuthUser?> ValidateResetTokenAsync(string token, CancellationToken ct);
    Task<bool> ResetPasswordWithTokenAsync(string token, string newPassword, CancellationToken ct);

}
