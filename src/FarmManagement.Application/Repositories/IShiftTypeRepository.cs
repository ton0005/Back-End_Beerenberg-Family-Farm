using FarmManagement.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FarmManagement.Application.Repositories
{
    public interface IShiftTypeRepository
    {
        Task<IEnumerable<ShiftType>> GetAllAsync();
        Task<ShiftType?> GetByIdAsync(int id);
        Task<ShiftType> AddAsync(ShiftType shiftType);
        Task<ShiftType?> UpdateAsync(ShiftType shiftType);
        Task<bool> DeleteAsync(int id);
        Task<ShiftType?> GetByTypeNameAsync(string typeName);
    }
}
