using FarmManagement.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Application.Repositories
{
    public interface IExceptionTypeRepository
    {
        Task<IEnumerable<ExceptionType>> GetAllAsync(CancellationToken ct = default);
    }
}