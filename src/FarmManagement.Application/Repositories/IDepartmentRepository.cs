using FarmManagement.Core.Entities;

namespace FarmManagement.Application.Repositories;

public interface IDepartmentRepository
{
    Task<int?> GetDepartmentIdByNameAsync(string departmentName, CancellationToken ct = default);
}
