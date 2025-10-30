using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities.Payroll;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class PayrollRunRepository : IPayrollRunRepository
    {
        private readonly ApplicationDbContext _db;

        public PayrollRunRepository(ApplicationDbContext db) => _db = db;

        public async Task<PayrollRun?> GetByIdAsync(int payrollRunId)
        {
            return await _db.PayrollRuns.FindAsync(payrollRunId);
        }

        public async Task<PayrollRun?> GetByIdWithDetailsAsync(int payrollRunId)
        {
            return await _db.PayrollRuns
                .Include(pr => pr.PayCalendar)
                .Include(pr => pr.LineItems)
                    .ThenInclude(li => li.Staff)
                .FirstOrDefaultAsync(pr => pr.PayrollRunId == payrollRunId);
        }

        public async Task<List<PayrollRun>> GetAllAsync()
        {
            return await _db.PayrollRuns
                .Include(pr => pr.PayCalendar)
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PayrollRun>> GetByPayCalendarIdAsync(int payCalendarId)
        {
            return await _db.PayrollRuns
                .Where(pr => pr.PayCalendarId == payCalendarId)
                .OrderByDescending(pr => pr.RunNumber)
                .ToListAsync();
        }

        public async Task<PayrollRun?> GetLatestByPayCalendarIdAsync(int payCalendarId)
        {
            return await _db.PayrollRuns
                .Where(pr => pr.PayCalendarId == payCalendarId)
                .OrderByDescending(pr => pr.RunNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PayrollRun>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _db.PayrollRuns
                .Include(pr => pr.PayCalendar)
                .Where(pr => pr.PayCalendar!.StartPeriodDate >= startDate && pr.PayCalendar.EndPeriodDate <= endDate)
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();
        }

        public async Task<PayrollRun> CreateAsync(PayrollRun payrollRun)
        {
            _db.PayrollRuns.Add(payrollRun);
            await _db.SaveChangesAsync();
            return payrollRun;
        }

        public async Task<PayrollRun> UpdateAsync(PayrollRun payrollRun)
        {
            _db.PayrollRuns.Update(payrollRun);
            await _db.SaveChangesAsync();
            return payrollRun;
        }

        public async Task<bool> DeleteAsync(int payrollRunId)
        {
            var payrollRun = await _db.PayrollRuns.FindAsync(payrollRunId);
            if (payrollRun == null) return false;

            _db.PayrollRuns.Remove(payrollRun);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
