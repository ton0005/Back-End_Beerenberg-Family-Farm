using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities.Payroll;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Data.Repositories
{
    public class PayCalendarRepository : IPayCalendarRepository
    {
        private readonly ApplicationDbContext _db;

        public PayCalendarRepository(ApplicationDbContext db) => _db = db;

        public async Task<PayCalendar?> GetByIdAsync(int payCalendarId)
        {
            return await _db.PayCalendars
                .Include(pc => pc.PayrollRuns)
                .FirstOrDefaultAsync(pc => pc.PayCalendarId == payCalendarId);
        }

        public async Task<List<PayCalendar>> GetAllAsync()
        {
            return await _db.PayCalendars
                .OrderByDescending(pc => pc.StartPeriodDate)
                .ToListAsync();
        }

        public async Task<List<PayCalendar>> GetActiveCalendarsAsync()
        {
            return await _db.PayCalendars
                .Where(pc => pc.Status == "Active")
                .OrderByDescending(pc => pc.StartPeriodDate)
                .ToListAsync();
        }

        public async Task<PayCalendar?> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _db.PayCalendars
                .FirstOrDefaultAsync(pc => pc.StartPeriodDate == startDate && pc.EndPeriodDate == endDate);
        }

        public async Task<bool> HasOverlappingCalendarAsync(DateTime startDate, DateTime endDate, int? excludeId = null)
        {
            var query = _db.PayCalendars.AsQueryable();

            if (excludeId.HasValue)
            {
                query = query.Where(pc => pc.PayCalendarId != excludeId.Value);
            }

            return await query.AnyAsync(pc =>
                (startDate >= pc.StartPeriodDate && startDate <= pc.EndPeriodDate) ||
                (endDate >= pc.StartPeriodDate && endDate <= pc.EndPeriodDate) ||
                (startDate <= pc.StartPeriodDate && endDate >= pc.EndPeriodDate));
        }

        public async Task<PayCalendar> CreateAsync(PayCalendar payCalendar)
        {
            _db.PayCalendars.Add(payCalendar);
            await _db.SaveChangesAsync();
            return payCalendar;
        }

        public async Task<PayCalendar> UpdateAsync(PayCalendar payCalendar)
        {
            _db.PayCalendars.Update(payCalendar);
            await _db.SaveChangesAsync();
            return payCalendar;
        }

        public async Task<bool> DeleteAsync(int payCalendarId)
        {
            var payCalendar = await _db.PayCalendars.FindAsync(payCalendarId);
            if (payCalendar == null) return false;

            _db.PayCalendars.Remove(payCalendar);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
