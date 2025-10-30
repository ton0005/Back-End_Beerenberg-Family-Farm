using FarmManagement.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Application.Repositories
{
    public interface IExceptionRepository
    {
        Task AddAsync(ExceptionLog log, CancellationToken ct = default);
        Task<ExceptionLog?> GetByIdAsync(int id, CancellationToken ct = default);
        Task UpdateAsync(ExceptionLog log, CancellationToken ct = default);
        Task<IEnumerable<ExceptionLog>> GetByStaffNumberAndDateAsync(string staffNumber, System.DateOnly date, CancellationToken ct = default);
    }
}
