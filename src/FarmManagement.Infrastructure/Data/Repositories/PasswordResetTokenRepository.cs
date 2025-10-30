using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;
using FarmManagement.Application.Repositories;
using FarmManagement.Infrastructure.Data;

namespace FarmManagement.Infrastructure.Data.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context;

    public PasswordResetTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct)
    {
        await _context.PasswordResetTokens.AddAsync(token, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct)
    {
        _context.PasswordResetTokens.Update(token);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<PasswordResetToken?> GetActiveTokenByStaffNumberAsync(string staffNumber, CancellationToken ct)
    {
        return await _context.PasswordResetTokens
            .Where(t => t.StaffNumber == staffNumber && !t.IsUsed && !t.IsExpired)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
}
