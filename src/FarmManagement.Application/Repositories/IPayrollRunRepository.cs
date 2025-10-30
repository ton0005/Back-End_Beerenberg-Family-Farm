using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmManagement.Core.Entities.Payroll;

namespace FarmManagement.Application.Repositories
{
    public interface IPayrollRunRepository
    {
        Task<PayrollRun?> GetByIdAsync(int payrollRunId);
        Task<PayrollRun?> GetByIdWithDetailsAsync(int payrollRunId);
        Task<List<PayrollRun>> GetAllAsync();
        Task<List<PayrollRun>> GetByPayCalendarIdAsync(int payCalendarId);
        Task<PayrollRun?> GetLatestByPayCalendarIdAsync(int payCalendarId);
        Task<List<PayrollRun>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<PayrollRun> CreateAsync(PayrollRun payrollRun);
        Task<PayrollRun> UpdateAsync(PayrollRun payrollRun);
        Task<bool> DeleteAsync(int payrollRunId);
    }
}
