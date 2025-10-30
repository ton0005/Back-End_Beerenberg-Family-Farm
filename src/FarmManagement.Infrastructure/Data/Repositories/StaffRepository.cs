using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Application.DTOs;
using System.Linq;
using FarmManagement.Core.Entities.Identity;

namespace FarmManagement.Infrastructure.Data.Repositories;

public class StaffRepository : IStaffRepository
{
    private readonly ApplicationDbContext _db;

    public StaffRepository(ApplicationDbContext db) => _db = db;

  public async Task<FarmManagement.Application.DTOs.PagedResult<Staff>> GetAllAsync(
    int page = 1,
    int pageSize = 20,
    int? departmentId = null,
    bool? isActive = null,
    string? search = null,
    string? staffNumber = null,
    string? sortBy = null,
    bool sortDesc = false,
    CancellationToken ct = default)
{
    var q = _db.Staff.Include(s => s.Department).AsNoTracking().AsQueryable();
    
    // Filtering
    if (departmentId.HasValue)
        q = q.Where(s => s.DepartmentId == departmentId.Value);

    // FIXED: Apply the same active/inactive logic as the controller
    if (isActive.HasValue)
    {
        if (isActive.Value)
        {
            // Active: no termination date OR termination date in future
            q = q.Where(s => !s.TerminationDate.HasValue || s.TerminationDate.Value > DateTime.UtcNow);
        }
        else
        {
            // Inactive: has past termination date OR IsActive = false
            q = q.Where(s => (s.TerminationDate.HasValue && s.TerminationDate.Value <= DateTime.UtcNow) || s.IsActive == false);
        }
    }

    if (!string.IsNullOrWhiteSpace(staffNumber))
    {
        var sn = staffNumber.Trim();
        var snLower = sn.ToLowerInvariant();
        q = q.Where(s => (s.StaffNumber ?? string.Empty).ToLower().Contains(snLower));
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim();
        // dynamic Where across multiple fields
        q = q.Where(x => (x.FirstName ?? string.Empty).Contains(s) || (x.LastName ?? string.Empty).Contains(s) || (x.Email ?? string.Empty).Contains(s) || (x.StaffNumber ?? string.Empty).Contains(s));
    }

    // explicit sorting: support a small set of known sortable fields to avoid dynamic LINQ
    if (!string.IsNullOrWhiteSpace(sortBy))
    {
        var sort = sortBy.Trim().ToLowerInvariant();
        bool desc = sortDesc;
        switch (sort)
        {
            case "lastname":
            case "last_name":
            case "last":
                q = desc ? q.OrderByDescending(x => x.LastName).ThenByDescending(x => x.FirstName) : q.OrderBy(x => x.LastName).ThenBy(x => x.FirstName);
                break;
            case "firstname":
            case "first_name":
            case "first":
                q = desc ? q.OrderByDescending(x => x.FirstName) : q.OrderBy(x => x.FirstName);
                break;
            case "staffnumber":
            case "staff_number":
                q = desc ? q.OrderByDescending(x => x.StaffNumber) : q.OrderBy(x => x.StaffNumber);
                break;
            case "hiredate":
            case "hire_date":
                q = desc ? q.OrderByDescending(x => x.HireDate) : q.OrderBy(x => x.HireDate);
                break;
            default:
                // fallback to default ordering if invalid sort expression
                q = q.OrderBy(x => x.LastName).ThenBy(x => x.FirstName);
                break;
        }
    }
    else
    {
        q = q.OrderBy(x => x.LastName).ThenBy(x => x.FirstName);
    }

    var total = await q.LongCountAsync(ct);

    var pageSafe = Math.Max(1, page);
    var pageSizeSafe = Math.Clamp(pageSize, 1, 200);

    var items = await q.Skip((pageSafe - 1) * pageSizeSafe).Take(pageSizeSafe).ToListAsync(ct);

    return new FarmManagement.Application.DTOs.PagedResult<Staff>
    {
        Page = page,
        PageSize = pageSize,
        TotalCount = total,
        Items = items
    };
}

    public async Task<Staff?> GetByIdAsync(int staffId, CancellationToken ct = default)
    {
        return await _db.Staff.Include(s => s.Department).FirstOrDefaultAsync(s => s.StaffId == staffId, ct);
    }

    public async Task<Staff?> GetByStaffNumberAsync(string staffNumber, CancellationToken ct = default)
    {
        var sn = (staffNumber ?? string.Empty).Trim();
        return await _db.Staff.Include(s => s.Department)
            .FirstOrDefaultAsync(s => (s.StaffNumber ?? string.Empty).Trim() == sn, ct);
    }

    public async Task AddAsync(Staff staff, CancellationToken ct = default)
    {
        _db.Staff.Add(staff);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Staff staff, CancellationToken ct = default)
    {
        _db.Staff.Update(staff);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Staff staff, CancellationToken ct = default)
    {
        // Remove any PasswordResetTokens that reference this staff's StaffNumber first
        if (!string.IsNullOrWhiteSpace(staff.StaffNumber))
        {
            var tokens = _db.PasswordResetTokens.Where(t => t.StaffNumber == staff.StaffNumber);
            _db.PasswordResetTokens.RemoveRange(tokens);
        }

        // Remove any AuthUser entries referencing this staff
        var authUsers = _db.Set<FarmManagement.Core.Entities.AuthUser>().Where(a => a.StaffId == staff.StaffId);
        _db.Set<FarmManagement.Core.Entities.AuthUser>().RemoveRange(authUsers);

        // Remove any Identity ApplicationUser linked to this staff (AspNetUsers)
        var appUsers = _db.Set<ApplicationUser>().Where(u => u.StaffId == staff.StaffId);
        _db.Set<ApplicationUser>().RemoveRange(appUsers);

        _db.Staff.Remove(staff);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<Staff>> GetStaffsAsync(IReadOnlyCollection<string> staffNumbers)
    {
        return await _db.Staff.Where(s => staffNumbers.Contains(s.StaffNumber)).ToArrayAsync();
    }
}
