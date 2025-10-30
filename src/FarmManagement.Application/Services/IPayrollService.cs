using FarmManagement.Application.DTOs;
using FarmManagement.Core.Entities.Payroll;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FarmManagement.Application.Services
{
    public interface IPayrollService
    {
        // Pay Calendar Operations
        Task<PayCalendarDto> CreatePayCalendarAsync(CreatePayCalendarRequest request, string createdBy);
        Task<List<PayCalendarListResponse>> GetAllPayCalendarsAsync();
        Task<PayCalendarDto?> GetPayCalendarByIdAsync(int payCalendarId);

        // Payroll Run Operations
        Task<PayrollRunDto> CreatePayrollAsync(int payCalendarId, string createdBy);
        Task<PayrollRunDto?> GetPayrollRunByIdAsync(int payrollRunId);
        Task<List<PayrollSummaryDto>> GetAllPayrollHistoryAsync();
        Task<List<PayrollSummaryDto>> GetPayrollHistoryByCalendarAsync(int payCalendarId);

        // Pay Rates
        Task<List<PayRateDto>> GetAllPayRatesAsync();
        Task InitializePayRatesAsync();
    }
}
