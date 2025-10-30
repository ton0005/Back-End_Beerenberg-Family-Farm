using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmManagement.Core.Entities.Payroll;

namespace FarmManagement.Application.Repositories
{
    public interface IPayCalendarRepository
    {
        Task<PayCalendar?> GetByIdAsync(int payCalendarId);
        Task<List<PayCalendar>> GetAllAsync();
        Task<List<PayCalendar>> GetActiveCalendarsAsync();
        Task<PayCalendar?> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> HasOverlappingCalendarAsync(DateTime startDate, DateTime endDate, int? excludeId = null);
        Task<PayCalendar> CreateAsync(PayCalendar payCalendar);
        Task<PayCalendar> UpdateAsync(PayCalendar payCalendar);
        Task<bool> DeleteAsync(int payCalendarId);
    }
}
