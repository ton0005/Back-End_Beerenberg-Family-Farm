using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using FarmManagement.Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class ShiftAssignmentRepository : IShiftAssignmentRepository
    {
        private readonly ApplicationDbContext _db;
        public ShiftAssignmentRepository(ApplicationDbContext db) { _db = db; }

        public void Add(ShiftAssignment assignment)
        {
            // If CompletedAt provided, mark assignment Completed
            if (assignment.CompletedAt.HasValue)
            {
                assignment.Status = AssignmentStatusEnum.Completed;
            }
            _db.ShiftAssignments.Add(assignment);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var a = await _db.ShiftAssignments.FindAsync(id);
            if (a == null) return false;
            _db.ShiftAssignments.Remove(a);

            if (!await _db.ShiftAssignments.AnyAsync(x => x.ShiftId == a.ShiftId && x.ShiftAssignmentId != id))
            {
                var shift = await _db.Shifts
                    .FirstOrDefaultAsync(x => x.ShiftId == a.ShiftId);
                if (shift != null)
                {
                    _db.Shifts.Remove(shift);
                }
            }
            
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<ShiftAssignment?> GetByIdAsync(int id)
        {
            return await _db.ShiftAssignments.Include(a => a.Shift).FirstOrDefaultAsync(a => a.ShiftAssignmentId == id);
        }

        public async Task<IEnumerable<ShiftAssignment>> GetByShiftIdAsync(int shiftId)
        {
            return await _db.ShiftAssignments
                .Where(a => a.ShiftId == shiftId)
                .Include(a => a.Shift)
                .Include(a => a.Staff)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<ShiftAssignment>> GetByStaffIdAsync(int staffId)
        {
            return await _db.ShiftAssignments.Where(a => a.StaffId == staffId).Include(a => a.Shift).AsNoTracking().ToListAsync();
        }

        public async Task<FarmManagement.Application.DTOs.PagedResult<ShiftAssignment>> GetByStaffIdPagedAsync(int staffId, int page = 1, int pageSize = 20, System.DateTime? startDate = null, System.DateTime? endDate = null, int? shiftTypeId = null, string? search = null, string? sortBy = null, bool sortDesc = false, System.Threading.CancellationToken ct = default)
        {
            // Build query without dereferencing the Shift navigation; use subqueries over Shifts instead
            var q = _db.ShiftAssignments
                .Where(a => a.StaffId == staffId)
                .Where(a => _db.Shifts.Any(s => s.ShiftId == a.ShiftId && s.IsPublished))
                .AsQueryable();

            if (startDate.HasValue)
            {
                var sd = startDate.Value.Date;
                q = q.Where(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.Date).FirstOrDefault() >= sd);
            }
            if (endDate.HasValue)
            {
                var ed = endDate.Value.Date;
                q = q.Where(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.Date).FirstOrDefault() <= ed);
            }
            if (shiftTypeId.HasValue)
            {
                var stid = shiftTypeId.Value;
                q = q.Where(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.ShiftTypeId).FirstOrDefault() == stid);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(a => (((_db.Shifts.Where(sh => sh.ShiftId == a.ShiftId).Select(sh => sh.Note).FirstOrDefault()) ?? string.Empty).ToLower()).Contains(s));
            }

            // Sorting using correlated subqueries to Shift fields
            switch (sortBy?.ToLower())
            {
                case "date":
                    q = sortDesc
                        ? q.OrderByDescending(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.Date).FirstOrDefault())
                        : q.OrderBy(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.Date).FirstOrDefault());
                    break;
                case "starttime":
                    q = sortDesc
                        ? q.OrderByDescending(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.StartTime).FirstOrDefault())
                        : q.OrderBy(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.StartTime).FirstOrDefault());
                    break;
                case "endtime":
                    q = sortDesc
                        ? q.OrderByDescending(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.EndTime).FirstOrDefault())
                        : q.OrderBy(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.EndTime).FirstOrDefault());
                    break;
                case "assignedat":
                    q = sortDesc ? q.OrderByDescending(a => a.AssignedAt) : q.OrderBy(a => a.AssignedAt);
                    break;
                default:
                    q = q
                        .OrderBy(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.Date).FirstOrDefault())
                        .ThenBy(a => _db.Shifts.Where(s => s.ShiftId == a.ShiftId).Select(s => s.StartTime).FirstOrDefault());
                    break;
            }

            var total = await q.CountAsync(ct);
            var items = await q
                .Include(a => a.Shift)
                .Include("Shift.ShiftType")
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new FarmManagement.Application.DTOs.PagedResult<ShiftAssignment>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };
        }

        public async Task<bool> IsAssignmentOverlappedAsync(int staffId, DateTime date, TimeSpan start, TimeSpan end)
        {
            return await _db.ShiftAssignments
                .Where(a => a.StaffId == staffId 
                            && a.Shift != null && a.Shift.Date == date
                            && (a.Shift.StartTime >= start && a.Shift.StartTime <= end
                                || a.Shift.EndTime >= start && a.Shift.EndTime <= end
                                || a.Shift.StartTime <= start && a.Shift.EndTime >= end))
                .AsNoTracking()
                .AnyAsync();
        }

        public async Task<ShiftAssignment?> UpdateAsync(ShiftAssignment assignment)
        {
            // If CompletedAt provided, ensure status reflects completion
            if (assignment.CompletedAt.HasValue)
            {
                assignment.Status = AssignmentStatusEnum.Completed;
            }
            else
            {
                // If CompletedAt cleared, revert back to Assigned (but don't override Removed)
                if (assignment.Status == AssignmentStatusEnum.Completed)
                {
                    assignment.Status = AssignmentStatusEnum.Assigned;
                }
            }

            _db.ShiftAssignments.Update(assignment);
            await _db.SaveChangesAsync();
            return assignment;
        }
    }
}
