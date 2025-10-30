using FarmManagement.Application.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FarmManagement.Application.Services
{
    /// <summary>
    /// Background service that automatically generates payroll runs for completed pay calendars
    /// </summary>
    public class PayrollAutoGenerationService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PayrollAutoGenerationService> _logger;
        private Timer? _timer;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public PayrollAutoGenerationService(
            IServiceProvider serviceProvider,
            ILogger<PayrollAutoGenerationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Payroll Auto Generation Service started");
            
            // Run immediately on startup, then every hour
            _timer = new Timer(
                async _ => await CheckAndGeneratePayrollsAsync(),
                null,
                TimeSpan.Zero,
                _checkInterval);
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Payroll Auto Generation Service stopped");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async Task CheckAndGeneratePayrollsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var payCalendarRepo = scope.ServiceProvider.GetRequiredService<IPayCalendarRepository>();
            var payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();

            var now = DateTime.UtcNow.Date;

            // Get all active pay calendars that have finished but don't have payroll generated
            var calendars = await payCalendarRepo.GetAllAsync();
            var completedCalendars = calendars.Where(c => 
                c.Status == "Active" &&
                !c.IsPayrollGenerated &&
                c.EndPeriodDate.Date < now
            ).ToList();

            if (!completedCalendars.Any())
            {
                _logger.LogDebug("No completed pay calendars found that need payroll generation");
                return;
            }

            _logger.LogInformation(
                "Found {Count} completed pay calendar(s) that need payroll generation",
                completedCalendars.Count);

            foreach (var calendar in completedCalendars)
            {
                try
                {
                    _logger.LogInformation(
                        "Auto-generating payroll for pay calendar {PayCalendarId} ({StartDate} - {EndDate})",
                        calendar.PayCalendarId,
                        calendar.StartPeriodDate,
                        calendar.EndPeriodDate);

                    var payrollRun = await payrollService.CreatePayrollAsync(
                        calendar.PayCalendarId,
                        "System-AutoGeneration");

                    _logger.LogInformation(
                        "Successfully auto-generated payroll run {PayrollRunId} for pay calendar {PayCalendarId}. " +
                        "Total cost: {TotalCost}, Staff count: {StaffCount}",
                        payrollRun.PayrollRunId,
                        calendar.PayCalendarId,
                        payrollRun.TotalLabourCost,
                        payrollRun.StaffCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to auto-generate payroll for pay calendar {PayCalendarId}",
                        calendar.PayCalendarId);
                }
            }
        }
    }
}
