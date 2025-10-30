using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class ExceptionRepository : IExceptionRepository
    {
        private readonly ApplicationDbContext _db;
        public ExceptionRepository(ApplicationDbContext db) => _db = db;

        public async Task AddAsync(ExceptionLog log, CancellationToken ct = default)
        {
            _db.ExceptionLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<ExceptionLog>> GetByStaffNumberAndDateAsync(string staffNumber, System.DateOnly date, CancellationToken ct = default)
        {
            var sn = (staffNumber ?? string.Empty).Trim();
            return await _db.ExceptionLogs.Where(e => e.StaffNumber == sn && e.ExceptionDate == date).ToListAsync(ct);
        }

        public async Task<ExceptionLog?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var entry = await _db.ExceptionLogs.FindAsync(new object[] { id });
            return entry as ExceptionLog;
        }

        public async Task UpdateAsync(ExceptionLog log, CancellationToken ct = default)
        {
            _db.ExceptionLogs.Update(log);
            await _db.SaveChangesAsync(ct);
        }
    }
}
