using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class ShiftTypeRepository : IShiftTypeRepository
    {
        private readonly ApplicationDbContext _db;
        public ShiftTypeRepository(ApplicationDbContext db) { _db = db; }

        public async Task<ShiftType> AddAsync(ShiftType shiftType)
        {
            _db.ShiftTypes.Add(shiftType);
            await _db.SaveChangesAsync();
            return shiftType;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var s = await _db.ShiftTypes.FindAsync(id);
            if (s == null) return false;
            _db.ShiftTypes.Remove(s);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<ShiftType?> GetByTypeNameAsync(string typeName)
        {
            return await _db.ShiftTypes.FirstOrDefaultAsync(s => s.Name == typeName);
        }

        public async Task<IEnumerable<ShiftType>> GetAllAsync()
        {
            return await _db.ShiftTypes.AsNoTracking().ToListAsync();
        }

        public async Task<ShiftType?> GetByIdAsync(int id)
        {
            return await _db.ShiftTypes.FindAsync(id);
        }

        public async Task<ShiftType?> UpdateAsync(ShiftType shiftType)
        {
            _db.ShiftTypes.Update(shiftType);
            await _db.SaveChangesAsync();
            return shiftType;
        }
    }
}
