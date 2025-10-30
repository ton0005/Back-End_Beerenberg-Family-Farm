using FarmManagement.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FarmManagement.Application.Repositories
{
    public interface IShiftRepository
    {
        Task<IEnumerable<Shift>> GetByDateAsync(System.DateTime date);
        Task<Shift?> GetByIdAsync(int id);
        void Add(Shift shift);
        Task<Shift?> UpdateAsync(Shift shift);
        Task<bool> DeleteAsync(int id);
        Task<Application.DTOs.PagedResult<Shift>> GetAllAsync(
            int page = 1,
            int pageSize = 20,
            System.DateTime? startDate = null,
            System.DateTime? endDate = null,
            int? shiftTypeId = null,
            string? search = null,
            string? sortBy = null,
            bool sortDesc = false,
            bool onlyPublished = false,
            System.Threading.CancellationToken ct = default);
        Task<int> SaveChangesAsync();
    }
}
