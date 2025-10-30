using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class ExceptionTypeRepository : IExceptionTypeRepository
    {
        private readonly ApplicationDbContext _db;
        public ExceptionTypeRepository(ApplicationDbContext db) => _db = db;

        public async Task<IEnumerable<ExceptionType>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.ExceptionTypes
                .OrderBy(e => e.TypeName)
                .ToListAsync(ct);
        }
    }
}