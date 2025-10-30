using FarmManagement.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FarmManagement.Application.Repositories
{
    public interface IShiftAssignmentRepository
    {
        Task<IEnumerable<ShiftAssignment>> GetByStaffIdAsync(int staffId);
        Task<FarmManagement.Application.DTOs.PagedResult<FarmManagement.Core.Entities.ShiftAssignment>> GetByStaffIdPagedAsync(int staffId, int page = 1, int pageSize = 20, DateTime? startDate = null, DateTime? endDate = null, int? shiftTypeId = null, string? search = null, string? sortBy = null, bool sortDesc = false, System.Threading.CancellationToken ct = default);
        Task<IEnumerable<ShiftAssignment>> GetByShiftIdAsync(int shiftId);
        Task<ShiftAssignment?> GetByIdAsync(int id);
        void Add(ShiftAssignment assignment);
        Task<ShiftAssignment?> UpdateAsync(ShiftAssignment assignment);
        Task<bool> DeleteAsync(int id);

        // Helper to find existing assignments for a staff between two datetimes (for overlap checks)
        Task<bool> IsAssignmentOverlappedAsync(int staffId, DateTime date, TimeSpan start, TimeSpan end);
    }
}
