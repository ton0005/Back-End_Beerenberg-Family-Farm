using FarmManagement.Core.Entities;
using FarmManagement.Application.DTOs;

namespace FarmManagement.Application.Repositories;

public interface IStaffRepository
{
    Task<FarmManagement.Application.DTOs.PagedResult<Staff>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        int? departmentId = null,
        bool? isActive = null,
        string? search = null,
        string? staffNumber = null,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken ct = default
    );
    Task<Staff?> GetByIdAsync(int staffId, CancellationToken ct = default);
    Task<Staff?> GetByStaffNumberAsync(string staffNumber, CancellationToken ct = default);
    Task AddAsync(Staff staff, CancellationToken ct = default);
    Task UpdateAsync(Staff staff, CancellationToken ct = default);
    Task DeleteAsync(Staff staff, CancellationToken ct = default);
    Task<IReadOnlyCollection<Staff>> GetStaffsAsync(IReadOnlyCollection<string> staffNumbers);
}
