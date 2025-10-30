using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;
using FarmManagement.Application.Repositories;

namespace FarmManagement.Infrastructure.Data.Repositories;

public class StaffRoleRepository : IStaffRoleRepository
{
    private readonly ApplicationDbContext _context;

    public StaffRoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(StaffRole staffRole, CancellationToken ct = default)
    {
        _context.StaffRoles.Add(staffRole);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<StaffRole>> GetRolesByStaffIdAsync(int staffId, CancellationToken ct = default)
    {
        return await _context.StaffRoles
            .Include(sr => sr.Role) // Include the Role details
            .Where(sr => sr.StaffId == staffId)
            .ToListAsync(ct);
    }

    public async Task<string?> GetRoleNameByIdAsync(int roleId, CancellationToken ct = default)
    {
        var role = await _context.AppRoles
            .Where(r => r.RoleId == roleId)
            .Select(r => r.RoleName)
            .FirstOrDefaultAsync(ct);
        
        return role;
    }

    public async Task<int?> GetRoleIdByNameAsync(string roleName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return null;
        var nameLower = roleName.Trim().ToLowerInvariant();
        // Case-insensitive lookup
        var id = await _context.AppRoles
            .Where(r => r.RoleName.ToLower() == nameLower)
            .Select(r => (int?)r.RoleId)
            .FirstOrDefaultAsync(ct);
        return id;
    }
}
