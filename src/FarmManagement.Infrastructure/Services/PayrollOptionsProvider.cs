using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FarmManagement.Application.Configuration;
using FarmManagement.Application.Services;
using FarmManagement.Core.Entities.Payroll;
using FarmManagement.Infrastructure.Data;

namespace FarmManagement.Infrastructure.Services
{
    public class PayrollOptionsProvider : IPayrollOptionsProvider
    {
        private readonly ApplicationDbContext _db;
        private readonly PayrollOptions _fallback;

        public PayrollOptionsProvider(ApplicationDbContext db, IOptions<PayrollOptions> fallback)
        {
            _db = db;
            _fallback = fallback?.Value ?? new PayrollOptions();
        }

        public async Task<PayrollOptions> GetOptionsAsync()
        {
            var entity = await _db.Set<PayrollOptionEntity>().OrderByDescending(p => p.Id).FirstOrDefaultAsync();
            if (entity == null)
            {
                return _fallback;
            }

            return new PayrollOptions
            {
                PayFrequency = entity.PayFrequency ?? _fallback.PayFrequency,
                FortnightlyDays = entity.FortnightlyDays ?? _fallback.FortnightlyDays ?? 14,
                CasualOvertimeThresholdHours = entity.CasualOvertimeThresholdHours ?? _fallback.CasualOvertimeThresholdHours ?? 12,
                PaidBreakMinutes = entity.PaidBreakMinutes ?? _fallback.PaidBreakMinutes
            };
        }
    }
}
