using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class ShiftRepository : IShiftRepository
    {
        private readonly ApplicationDbContext _db;
        public ShiftRepository(ApplicationDbContext db) { _db = db; }

        public void Add(Shift shift)
        {
            _db.Shifts.Add(shift);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var s = await _db.Shifts.FindAsync(id);
            if (s == null) return false;
            _db.Shifts.Remove(s);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<Shift?> GetByIdAsync(int id)
        {
            return await _db.Shifts.Include(s => s.ShiftType).FirstOrDefaultAsync(s => s.ShiftId == id);
        }

        public async Task<IEnumerable<Shift>> GetByDateAsync(System.DateTime date)
        {
            var d = date.Date;
            return await _db.Shifts.Where(s => s.Date == d).AsNoTracking().ToListAsync();
        }

        public async Task<Shift?> UpdateAsync(Shift shift)
        {
            _db.Shifts.Update(shift);
            await _db.SaveChangesAsync();
            return shift;
        }

        public async Task<FarmManagement.Application.DTOs.PagedResult<Shift>> GetAllAsync(
            int page = 1,
            int pageSize = 20,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? shiftTypeId = null,
            string? search = null,
            string? sortBy = null,
            bool sortDesc = false,
            bool onlyPublished = false,
            CancellationToken ct = default)
        {
            var q = _db.Shifts.Include(s => s.ShiftType).AsQueryable();
            if (onlyPublished)
            {
                q = q.Where(s => s.IsPublished);
            }
            if (startDate.HasValue)
            {
                var sd = startDate.Value.Date;
                q = q.Where(s => s.Date >= sd);
            }
            if (endDate.HasValue)
            {
                var ed = endDate.Value.Date;
                q = q.Where(s => s.Date <= ed);
            }
            if (shiftTypeId.HasValue)
            {
                q = q.Where(s => s.ShiftTypeId == shiftTypeId.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(sft => (sft.Note ?? "").ToLower().Contains(s));
            }

            // Sorting
            switch (sortBy?.ToLower())
            {
                case "date":
                    q = sortDesc ? q.OrderByDescending(s => s.Date) : q.OrderBy(s => s.Date); break;
                case "starttime":
                    q = sortDesc ? q.OrderByDescending(s => s.StartTime) : q.OrderBy(s => s.StartTime); break;
                case "endtime":
                    q = sortDesc ? q.OrderByDescending(s => s.EndTime) : q.OrderBy(s => s.EndTime); break;
                case "shifttypeid":
                    q = sortDesc ? q.OrderByDescending(s => s.ShiftTypeId) : q.OrderBy(s => s.ShiftTypeId); break;
                default:
                    q = q.OrderBy(s => s.Date).ThenBy(s => s.StartTime); break;
            }

            var total = await q.CountAsync(ct);
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync(ct);
            return new FarmManagement.Application.DTOs.PagedResult<Shift>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }
    }
}
