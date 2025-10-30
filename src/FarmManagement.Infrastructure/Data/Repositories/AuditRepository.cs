using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly ApplicationDbContext _db;
        public AuditRepository(ApplicationDbContext db) => _db = db;

        public async Task AddAsync(AuditLog log, CancellationToken ct = default)
        {
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<AuditLog>> GetByTableAndRecordIdsAsync(string tableName, IEnumerable<int> recordIds, CancellationToken ct = default)
        {
            var ids = recordIds?.ToList() ?? new List<int>();
            if (!ids.Any()) return Enumerable.Empty<AuditLog>();

            return await _db.AuditLogs
                .Where(a => a.TableName == tableName && ids.Contains(a.RecordId))
                .OrderByDescending(a => a.PerformedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<FarmManagement.Application.DTOs.PagedResult<FarmManagement.Application.DTOs.AuditDto>> QueryAsync(FarmManagement.Application.DTOs.AuditQuery query, CancellationToken ct = default)
        {
            var q = _db.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.TableName)) q = q.Where(a => a.TableName == query.TableName);
            if (query.RecordIds != null && query.RecordIds.Length > 0) q = q.Where(a => query.RecordIds.Contains(a.RecordId));
            if (!string.IsNullOrWhiteSpace(query.CorrelationId)) q = q.Where(a => a.CorrelationId == query.CorrelationId);
            if (!string.IsNullOrWhiteSpace(query.ActionType)) q = q.Where(a => a.ActionType == query.ActionType);
            if (!string.IsNullOrWhiteSpace(query.PerformedBy)) q = q.Where(a => a.PerformedBy == query.PerformedBy);
            if (query.From.HasValue) q = q.Where(a => a.PerformedAt >= query.From.Value);
            if (query.To.HasValue) q = q.Where(a => a.PerformedAt <= query.To.Value);

            var total = await q.LongCountAsync(ct);

            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 1000);

            var items = await q
                .OrderByDescending(a => a.PerformedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new FarmManagement.Application.DTOs.AuditDto
                {
                    AuditId = a.AuditId,
                    TableName = a.TableName,
                    RecordId = a.RecordId,
                    ActionType = a.ActionType,
                    ChangesJson = a.ChangesJson == null ? null : (a.ChangesJson.Length > 200 ? a.ChangesJson.Substring(0, 200) + "..." : a.ChangesJson),
                    PerformedBy = a.PerformedBy,
                    PerformedAt = a.PerformedAt
                })
                .ToListAsync(ct);

            return new FarmManagement.Application.DTOs.PagedResult<FarmManagement.Application.DTOs.AuditDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };
        }
    }
}
