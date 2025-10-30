using FarmManagement.Core.Entities;

namespace FarmManagement.Application.Repositories;
public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct);
    Task AddAsync(PasswordResetToken token, CancellationToken ct);
    Task UpdateAsync(PasswordResetToken token, CancellationToken ct);
}