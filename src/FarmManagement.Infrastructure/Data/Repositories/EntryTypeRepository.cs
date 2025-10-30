using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class EntryTypeRepository : IEntryTypeRepository
    {
        private readonly ApplicationDbContext _db;
        public EntryTypeRepository(ApplicationDbContext db) => _db = db;

        public async Task<EntryType?> GetByIdAsync(int id)
        {
            return await _db.EntryTypes.FindAsync(id);
        }

        public async Task<EntryType?> GetByNameAsync(string name)
        {
            var n = (name ?? string.Empty).Trim().ToUpperInvariant();
            return await _db.EntryTypes.AsNoTracking().FirstOrDefaultAsync(e => e.TypeName.ToUpper() == n);
        }
    }
}
