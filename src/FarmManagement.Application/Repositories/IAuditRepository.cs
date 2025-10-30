using FarmManagement.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Application.Repositories
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog log, CancellationToken ct = default);
        Task<IEnumerable<AuditLog>> GetByTableAndRecordIdsAsync(string tableName, IEnumerable<int> recordIds, CancellationToken ct = default);
        Task<FarmManagement.Application.DTOs.PagedResult<FarmManagement.Application.DTOs.AuditDto>> QueryAsync(FarmManagement.Application.DTOs.AuditQuery query, CancellationToken ct = default);
    }
}
