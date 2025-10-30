using FarmManagement.Core.Entities;

namespace FarmManagement.Application.Repositories;

public interface IStaffRoleRepository
{
    Task AddAsync(StaffRole staffRole, CancellationToken ct = default);
    Task<IEnumerable<StaffRole>> GetRolesByStaffIdAsync(int staffId, CancellationToken ct = default);
    Task<string?> GetRoleNameByIdAsync(int roleId, CancellationToken ct = default);
    Task<int?> GetRoleIdByNameAsync(string roleName, CancellationToken ct = default);
}
