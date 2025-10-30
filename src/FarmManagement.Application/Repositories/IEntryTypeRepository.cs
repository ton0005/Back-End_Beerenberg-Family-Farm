using FarmManagement.Core.Entities;
using System.Threading.Tasks;

namespace FarmManagement.Application.Repositories
{
    public interface IEntryTypeRepository
    {
        Task<EntryType?> GetByIdAsync(int id);
        Task<EntryType?> GetByNameAsync(string name);
    }
}
