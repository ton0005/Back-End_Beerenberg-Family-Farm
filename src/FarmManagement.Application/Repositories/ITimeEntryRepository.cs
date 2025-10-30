using FarmManagement.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Application.Repositories
{
    public interface ITimeEntryRepository
    {
        Task AddAsync(TimeEntry entry, CancellationToken ct = default);
        Task<IEnumerable<TimeEntry>> GetByStaffNumberAsync(string staffNumber, CancellationToken ct = default);
        Task<TimeEntry?> GetByIdAsync(int id, CancellationToken ct = default);
        Task UpdateAsync(TimeEntry entry, CancellationToken ct = default);
        Task DeleteAsync(TimeEntry entry, CancellationToken ct = default);
        Task<IEnumerable<TimeEntry>> GetByStaffNumberAndDateAsync(string staffNumber, DateOnly date, CancellationToken ct = default);
        Task<List<TimeEntry>> GetByStaffNumberAndDateForUpdateAsync(string staffNumber, DateOnly date, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<TimeEntry> entries, CancellationToken ct = default);
        Task DeleteRangeAsync(IEnumerable<TimeEntry> entries, CancellationToken ct = default);
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitTransactionAsync(CancellationToken ct = default);
        Task RollbackTransactionAsync(CancellationToken ct = default);
        Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct = default);
        Task<FarmManagement.Application.DTOs.PagedResult<TimeEntry>> QueryAsync(string? staffNumber = null, int? entryTypeId = null, DateTime? start = null, DateTime? end = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<List<TimeEntry>> GetByStaffAndDateRangeAsync(string staffNumber, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    }
}
