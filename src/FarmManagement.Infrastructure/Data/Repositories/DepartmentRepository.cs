using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;
using FarmManagement.Application.Repositories;

namespace FarmManagement.Infrastructure.Data.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _db;
    public DepartmentRepository(ApplicationDbContext db) => _db = db;

    public async Task<int?> GetDepartmentIdByNameAsync(string departmentName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(departmentName)) return null;
        var nameLower = departmentName.Trim().ToLowerInvariant();
        // Case-insensitive match by normalizing both sides to lower-case
        var id = await _db.Departments
            .Where(d => d.DepartmentName.ToLower() == nameLower)
            .Select(d => (int?)d.DepartmentId)
            .FirstOrDefaultAsync(ct);
        return id; // will be null when no match
    }
}
