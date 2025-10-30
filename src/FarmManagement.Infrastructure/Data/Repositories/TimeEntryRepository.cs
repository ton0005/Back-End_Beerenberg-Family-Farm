using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class TimeEntryRepository : ITimeEntryRepository
    {
        private readonly ApplicationDbContext _db;

        public TimeEntryRepository(ApplicationDbContext db) => _db = db;

        public async Task AddAsync(TimeEntry entry, CancellationToken ct = default)
        {
            _db.TimeEntries.Add(entry);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(TimeEntry entry, CancellationToken ct = default)
        {
            _db.TimeEntries.Update(entry);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(TimeEntry entry, CancellationToken ct = default)
        {
            _db.TimeEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<TimeEntry>> GetByStaffNumberAsync(string staffNumber, CancellationToken ct = default)
        {
            var sn = (staffNumber ?? string.Empty).Trim();
            return await _db.TimeEntries.Where(t => t.StaffNumber == sn).ToListAsync(ct);
        }

        public async Task<IEnumerable<TimeEntry>> GetByStaffNumberAndDateAsync(string staffNumber, DateOnly date, CancellationToken ct = default)
        {
            var sn = (staffNumber ?? string.Empty).Trim();
            return await _db.TimeEntries.Where(t => t.StaffNumber == sn && t.EntryTimestamp.Date == date.ToDateTime(new TimeOnly(0)).Date).ToListAsync(ct);
        }

        public async Task<List<TimeEntry>> GetByStaffNumberAndDateForUpdateAsync(string staffNumber, DateOnly date, CancellationToken ct = default)
        {
            var sn = (staffNumber ?? string.Empty).Trim();
            // tracked for update/delete diff
            return await _db.TimeEntries.Where(t => t.StaffNumber == sn && t.EntryTimestamp.Date == date.ToDateTime(new TimeOnly(0)).Date).ToListAsync(ct);
        }

        public async Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct = default)
        {
            var startDt = start.ToDateTime(new TimeOnly(0));
            var endDt = end.ToDateTime(new TimeOnly(23, 59, 59, 999));
            return await _db.TimeEntries
                .AsNoTracking()
                .Where(t => t.EntryTimestamp >= startDt && t.EntryTimestamp <= endDt)
                .ToListAsync(ct);
        }

        public async Task<FarmManagement.Application.DTOs.PagedResult<TimeEntry>> QueryAsync(string? staffNumber = null, int? entryTypeId = null, DateTime? start = null, DateTime? end = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var q = _db.TimeEntries.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(staffNumber))
            {
                var sn = staffNumber.Trim();
                q = q.Where(t => t.StaffNumber == sn);
            }

            if (entryTypeId.HasValue)
            {
                q = q.Where(t => t.EntryTypeId == entryTypeId.Value);
            }

            if (start.HasValue)
            {
                q = q.Where(t => t.EntryTimestamp >= start.Value);
            }

            if (end.HasValue)
            {
                q = q.Where(t => t.EntryTimestamp <= end.Value);
            }

            var total = await q.LongCountAsync(ct);
            var items = await q.OrderByDescending(t => t.EntryTimestamp)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync(ct);

            return new FarmManagement.Application.DTOs.PagedResult<TimeEntry>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };
        }

        public async Task<TimeEntry?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.TimeEntries.FindAsync(new object[] { id }, ct);
        }

        public async Task AddRangeAsync(IEnumerable<TimeEntry> entries, CancellationToken ct = default)
        {
            await _db.TimeEntries.AddRangeAsync(entries, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteRangeAsync(IEnumerable<TimeEntry> entries, CancellationToken ct = default)
        {
            _db.TimeEntries.RemoveRange(entries);
            await _db.SaveChangesAsync(ct);
        }

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_db.Database.CurrentTransaction == null)
            {
                await _db.Database.BeginTransactionAsync(ct);
            }
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_db.Database.CurrentTransaction != null)
            {
                await _db.Database.CommitTransactionAsync(ct);
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (_db.Database.CurrentTransaction != null)
            {
                await _db.Database.RollbackTransactionAsync(ct);
            }
        }

        public async Task<List<TimeEntry>> GetByStaffAndDateRangeAsync(
            string staffNumber, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken ct = default)
        {
            return await _db.TimeEntries
                .Where(te => te.StaffNumber == staffNumber 
                    && te.EntryTimestamp >= startDate 
                    && te.EntryTimestamp < endDate)
                .OrderBy(te => te.EntryTimestamp)
                .ToListAsync(ct);
        }
    }
}
