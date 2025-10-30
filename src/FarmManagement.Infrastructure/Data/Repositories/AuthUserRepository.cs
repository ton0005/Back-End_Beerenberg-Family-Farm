using FarmManagement.Core.Entities;
using FarmManagement.Application.Repositories;
using FarmManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Data.Repositories;

public class AuthUserRepository : IAuthUserRepository
{
    private readonly ApplicationDbContext _context;

    public AuthUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuthUser?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        return await _context.AuthUsers
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => u.Username == username, ct);
    }

    public async Task<AuthUser?> GetByStaffIdAsync(int staffId, CancellationToken ct)
    {
        return await _context.AuthUsers
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => u.StaffId == staffId, ct);
    }

    public async Task<AuthUser?> GetByStaffNumberAsync(string staffNumber, CancellationToken ct)
    {
        // Normalize input first
        if (string.IsNullOrWhiteSpace(staffNumber)) return null;

        staffNumber = staffNumber.Trim();

        // First try a direct equality (fast path)
        var user = await _context.AuthUsers
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => u.Staff.StaffNumber == staffNumber, ct);

        if (user != null) return user;

        // Some databases may store StaffNumber as fixed-length (nchar) or with padding;
        // try a trimmed comparison on the DB column as a fallback. EF Core translates
        // .Trim() to SQL LTRIM/RTRIM for SQL Server.
        return await _context.AuthUsers
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => (u.Staff.StaffNumber ?? string.Empty).Trim() == staffNumber, ct);
    }

    public async Task AddAsync(AuthUser user, CancellationToken ct = default)
    {
        _context.AuthUsers.Add(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AuthUser user, CancellationToken ct = default)
    {
        _context.AuthUsers.Update(user);
        await _context.SaveChangesAsync(ct);
    }
}
