using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities.Payroll;
using FarmManagement.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class PayRateRepository : IPayRateRepository
    {
        private readonly ApplicationDbContext _db;

        public PayRateRepository(ApplicationDbContext db) => _db = db;

        public async Task<PayRate?> GetByIdAsync(int payRateId)
        {
            return await _db.PayRates.FindAsync(payRateId);
        }

        public async Task<List<PayRate>> GetAllAsync()
        {
            return await _db.PayRates
                .OrderByDescending(pr => pr.EffectiveFrom)
                .ToListAsync();
        }

        public async Task<List<PayRate>> GetActiveRatesAsync()
        {
            return await _db.PayRates
                .Where(pr => pr.IsActive)
                .ToListAsync();
        }

        public async Task<PayRate?> GetActiveRateAsync(ContractTypeEnum contractType, string rateType = "Regular")
        {
            return await _db.PayRates
                .Where(pr => pr.ContractType == contractType
                    && pr.RateType == rateType
                    && pr.IsActive
                    && (pr.EffectiveTo == null || pr.EffectiveTo > DateTime.UtcNow))
                .OrderByDescending(pr => pr.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<PayRate?> GetRateForDateAsync(ContractTypeEnum contractType, DateTime date, string rateType = "Regular")
        {
            return await _db.PayRates
                .Where(pr => pr.ContractType == contractType
                    && pr.RateType == rateType
                    && pr.EffectiveFrom <= date
                    && (pr.EffectiveTo == null || pr.EffectiveTo > date))
                .OrderByDescending(pr => pr.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<PayRate> CreateAsync(PayRate payRate)
        {
            _db.PayRates.Add(payRate);
            await _db.SaveChangesAsync();
            return payRate;
        }

        public async Task<PayRate> UpdateAsync(PayRate payRate)
        {
            _db.PayRates.Update(payRate);
            await _db.SaveChangesAsync();
            return payRate;
        }

        public async Task<bool> DeleteAsync(int payRateId)
        {
            var payRate = await _db.PayRates.FindAsync(payRateId);
            if (payRate == null) return false;

            _db.PayRates.Remove(payRate);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
