using FarmManagement.Core.Entities;

namespace FarmManagement.Application.Repositories;

public interface IAuthUserRepository
{
    Task<AuthUser?> GetByUsernameAsync(string username, CancellationToken ct);
    Task<AuthUser?> GetByStaffNumberAsync(string staffNumber, CancellationToken ct);

    // // Add new AuthUser into database
    Task AddAsync(AuthUser user, CancellationToken ct = default);
    // Update existing AuthUser in database for reseting password
    Task UpdateAsync(AuthUser user, CancellationToken ct = default);
}