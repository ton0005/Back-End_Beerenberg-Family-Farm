using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmManagement.Core.Entities.Payroll;
using FarmManagement.Core.Enums;

namespace FarmManagement.Application.Repositories
{
    public interface IPayRateRepository
    {
        Task<PayRate?> GetByIdAsync(int payRateId);
        Task<List<PayRate>> GetAllAsync();
        Task<List<PayRate>> GetActiveRatesAsync();
        Task<PayRate?> GetActiveRateAsync(ContractTypeEnum contractType, string rateType = "Regular");
        Task<PayRate?> GetRateForDateAsync(ContractTypeEnum contractType, DateTime date, string rateType = "Regular");
        Task<PayRate> CreateAsync(PayRate payRate);
        Task<PayRate> UpdateAsync(PayRate payRate);
        Task<bool> DeleteAsync(int payRateId);
    }
}
