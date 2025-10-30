using System.Threading.Tasks;
using FarmManagement.Application.Configuration;

namespace FarmManagement.Application.Services
{
    public interface IPayrollOptionsProvider
    {
        /// <summary>
        /// Returns effective PayrollOptions, sourcing from DB when available and falling back to configuration.
        /// </summary>
        Task<PayrollOptions> GetOptionsAsync();
    }
}
