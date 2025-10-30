using FarmManagement.Application.DTOs;
using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities.Payroll;
using FarmManagement.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FarmManagement.Application.Services
{
    public class PayrollService : IPayrollService
    {
        private readonly IPayCalendarRepository _payCalendarRepo;
        private readonly IPayrollRunRepository _payrollRunRepo;
        private readonly IPayRateRepository _payRateRepo;
        private readonly ITimeEntryRepository _timeEntryRepo;
        private readonly ITimeEntryService _timeEntryService;
        private readonly IStaffRepository _staffRepo;
        private readonly ILogger<PayrollService> _logger;

        private readonly IPayrollOptionsProvider _payrollOptionsProvider;

        public PayrollService(
            IPayCalendarRepository payCalendarRepo,
            IPayrollRunRepository payrollRunRepo,
            IPayRateRepository payRateRepo,
            ITimeEntryRepository timeEntryRepo,
            ITimeEntryService timeEntryService,
            IStaffRepository staffRepo,
            ILogger<PayrollService> logger,
            IPayrollOptionsProvider payrollOptionsProvider)
        {
            _payCalendarRepo = payCalendarRepo;
            _payrollRunRepo = payrollRunRepo;
            _payRateRepo = payRateRepo;
            _timeEntryRepo = timeEntryRepo;
            _timeEntryService = timeEntryService;
            _staffRepo = staffRepo;
            _logger = logger;
            _payrollOptionsProvider = payrollOptionsProvider;
        }

        #region Pay Calendar Operations

        public async Task<PayCalendarDto> CreatePayCalendarAsync(CreatePayCalendarRequest request, string createdBy)
        {
            var opts = await _payrollOptionsProvider.GetOptionsAsync();
            // Validate dates
            if (request.StartPeriodDate >= request.PayDate)
            {
                throw new InvalidOperationException("Pay date must be after the start period date");
            }

            // Auto-generate end period date based on configured pay period days
            var periodDays = opts.PayFrequency?.Equals("Fortnightly", StringComparison.OrdinalIgnoreCase) == true
                ? opts.FortnightlyDays ?? 14
                : opts.FortnightlyDays ?? 14; // default for now; future: support weekly/monthly

            var endPeriodDate = request.StartPeriodDate.AddDays(periodDays - 1);

            // Validate pay date is after end period
            if (request.PayDate <= endPeriodDate)
            {
                throw new InvalidOperationException("Pay date must be after the end period date");
            }

            // Check for overlapping calendars
            var hasOverlap = await _payCalendarRepo.HasOverlappingCalendarAsync(
                request.StartPeriodDate,
                endPeriodDate);

            if (hasOverlap)
            {
                throw new InvalidOperationException("Pay Calendar exists");
            }

            var payCalendar = new PayCalendar
            {
                StartPeriodDate = request.StartPeriodDate.Date,
                EndPeriodDate = endPeriodDate.Date,
                PayDate = request.PayDate.Date,
                PayFrequency = opts.PayFrequency ?? "Fortnightly",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            var created = await _payCalendarRepo.CreateAsync(payCalendar);

            return MapToDto(created);
        }

        public async Task<List<PayCalendarListResponse>> GetAllPayCalendarsAsync()
        {
            var calendars = await _payCalendarRepo.GetAllAsync();
            return calendars.Select(pc => new PayCalendarListResponse
            {
                PayCalendarId = pc.PayCalendarId,
                StartPeriodDate = pc.StartPeriodDate,
                EndPeriodDate = pc.EndPeriodDate,
                PayDate = pc.PayDate,
                PayFrequency = pc.PayFrequency,
                Status = pc.Status,
                IsPayrollGenerated = pc.IsPayrollGenerated
            }).ToList();
        }

        public async Task<PayCalendarDto?> GetPayCalendarByIdAsync(int payCalendarId)
        {
            var calendar = await _payCalendarRepo.GetByIdAsync(payCalendarId);
            return calendar != null ? MapToDto(calendar) : null;
        }

        #endregion

        #region Payroll Run Operations

        public async Task<PayrollRunDto> CreatePayrollAsync(int payCalendarId, string createdBy)
        {
            var payCalendar = await _payCalendarRepo.GetByIdAsync(payCalendarId);
            if (payCalendar == null)
            {
                throw new InvalidOperationException("Pay calendar not found");
            }

            // Check if end period has passed
            if (DateTime.UtcNow.Date < payCalendar.EndPeriodDate.Date)
            {
                throw new InvalidOperationException("Pay Period has not completed yet");
            }

            // Get or calculate run number
            var existingRuns = await _payrollRunRepo.GetByPayCalendarIdAsync(payCalendarId);
            var runNumber = existingRuns.Any() ? existingRuns.Max(r => r.RunNumber) + 1 : 1;

            // Calculate payroll for all active staff
            var lineItems = await CalculatePayrollLineItemsAsync(
                payCalendar.StartPeriodDate,
                payCalendar.EndPeriodDate);

            var payrollRun = new PayrollRun
            {
                PayCalendarId = payCalendarId,
                RunNumber = runNumber,
                TotalLabourCost = lineItems.Sum(li => li.NetWages),
                TotalWorkHours = lineItems.Sum(li => li.TotalHours),
                StaffCount = lineItems.Count,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                LineItems = lineItems
            };

            var created = await _payrollRunRepo.CreateAsync(payrollRun);

            // Update pay calendar
            payCalendar.IsPayrollGenerated = true;
            payCalendar.UpdatedAt = DateTime.UtcNow;
            payCalendar.UpdatedBy = createdBy;
            await _payCalendarRepo.UpdateAsync(payCalendar);

            _logger.LogInformation(
                "Payroll run created: PayrollRunId={PayrollRunId}, PayCalendarId={PayCalendarId}, TotalCost={TotalCost}, StaffCount={StaffCount}",
                created.PayrollRunId, payCalendarId, created.TotalLabourCost, created.StaffCount);

            return await MapToPayrollRunDto(created);
        }

        public async Task<PayrollRunDto?> GetPayrollRunByIdAsync(int payrollRunId)
        {
            var payrollRun = await _payrollRunRepo.GetByIdWithDetailsAsync(payrollRunId);
            return payrollRun != null ? await MapToPayrollRunDto(payrollRun) : null;
        }

        public async Task<List<PayrollSummaryDto>> GetAllPayrollHistoryAsync()
        {
            var payrollRuns = await _payrollRunRepo.GetAllAsync();
            return payrollRuns.Select(pr => new PayrollSummaryDto
            {
                PayrollRunId = pr.PayrollRunId,
                PayCalendarId = pr.PayCalendarId,
                PayCalendarPeriod = pr.PayCalendar != null
                    ? $"{pr.PayCalendar.StartPeriodDate:dd/MM/yyyy} - {pr.PayCalendar.EndPeriodDate:dd/MM/yyyy}"
                    : "N/A",
                TotalLabourCost = pr.TotalLabourCost,
                TotalWorkHours = pr.TotalWorkHours,
                StaffCount = pr.StaffCount,
                Status = pr.Status,
                CreatedAt = pr.CreatedAt
            }).ToList();
        }

        public async Task<List<PayrollSummaryDto>> GetPayrollHistoryByCalendarAsync(int payCalendarId)
        {
            var payrollRuns = await _payrollRunRepo.GetByPayCalendarIdAsync(payCalendarId);
            var payCalendar = await _payCalendarRepo.GetByIdAsync(payCalendarId);

            return payrollRuns.Select(pr => new PayrollSummaryDto
            {
                PayrollRunId = pr.PayrollRunId,
                PayCalendarId = pr.PayCalendarId,
                PayCalendarPeriod = payCalendar != null
                    ? $"{payCalendar.StartPeriodDate:dd/MM/yyyy} - {payCalendar.EndPeriodDate:dd/MM/yyyy}"
                    : "N/A",
                TotalLabourCost = pr.TotalLabourCost,
                TotalWorkHours = pr.TotalWorkHours,
                StaffCount = pr.StaffCount,
                Status = pr.Status,
                CreatedAt = pr.CreatedAt
            }).ToList();
        }

        #endregion

        #region Pay Rates

        public async Task<List<PayRateDto>> GetAllPayRatesAsync()
        {
            var rates = await _payRateRepo.GetAllAsync();
            return rates.Select(r => new PayRateDto
            {
                PayRateId = r.PayRateId,
                ContractType = r.ContractType.ToString(),
                RateType = r.RateType,
                HourlyRate = r.HourlyRate,
                EffectiveFrom = r.EffectiveFrom,
                EffectiveTo = r.EffectiveTo,
                IsActive = r.IsActive,
                Description = r.Description
            }).ToList();
        }

        public async Task InitializePayRatesAsync()
        {
            var existingRates = await _payRateRepo.GetActiveRatesAsync();
            if (existingRates.Any())
            {
                _logger.LogInformation("Pay rates already initialized");
                return;
            }
            // No active rates found in DB. This application expects pay rates to be seeded
            // via the data seed process (e.g. payroll_seed.json + PayRateSeeder) or managed
            // via an admin UI/API. Avoid hard-coding rates in code to keep data flexible.
            _logger.LogWarning("No active pay rates found. Please run the pay rate seeder or add rates via the admin API.");
            return;
        }

        #endregion

        #region Private Helper Methods

        private async Task<List<PayrollLineItem>> CalculatePayrollLineItemsAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var opts = await _payrollOptionsProvider.GetOptionsAsync();
            var lineItems = new List<PayrollLineItem>();

            // Get all active staff
            var staffResult = await _staffRepo.GetAllAsync(
                page: 1,
                pageSize: 10000,
                isActive: true);

            foreach (var staff in staffResult.Items)
            {
                // Use session-based worked minutes (calculated in TimeEntryService)
                // GetSessionsAsync signature: (staffNumber, date?, startDate?, endDate?)
                var sessions = await _timeEntryService.GetSessionsAsync(
                    staff.StaffNumber,
                    date: null,  // not filtering by single date
                    startDate: DateOnly.FromDateTime(startDate),
                    endDate: DateOnly.FromDateTime(endDate));

                if (sessions == null || !sessions.Any())
                {
                    continue; // Skip staff with no sessions
                }

                // Aggregate daily minutes: WorkedMinutes + 10-minute paid break for sessions > 4 hours
                var paidBreakMinutes = opts.PaidBreakMinutes; // configured paid break minutes (typically 10)
                var dailyMinutes = sessions
                    .Where(s => s.WorkedMinutes.HasValue)
                    .GroupBy(s => s.Date)
                    .ToDictionary(
                        g => g.Key.ToDateTime(TimeOnly.MinValue), 
                        g => (decimal)g.Sum(s => {
                            var worked = s.WorkedMinutes.GetValueOrDefault();
                            // Add paid break only if session lasted more than 4 hours (240 minutes)
                            var bonus = worked > 240 ? paidBreakMinutes : 0;
                            return worked + bonus;
                        })
                    );

                if (!dailyMinutes.Any()) continue;

                // Convert to hours per day for existing logic
                var dailyHours = dailyMinutes.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value / 60m, 4));

                // Get applicable pay rates
                var regularRate = await _payRateRepo.GetActiveRateAsync(staff.ContractType, "Regular");
                var overtimeRate = await _payRateRepo.GetActiveRateAsync(staff.ContractType, "Overtime");

                if (regularRate == null)
                {
                    _logger.LogWarning(
                        "No regular pay rate found for staff {StaffNumber} with contract type {ContractType}",
                        staff.StaffNumber, staff.ContractType);
                    continue;
                }

                decimal regularHours = 0;
                decimal overtimeHours = 0;

                // Calculate regular and overtime hours
                if (staff.ContractType == ContractTypeEnum.Casual)
                {
                    // For casual: configured threshold hours per day is overtime
                    var casualThreshold = opts.CasualOvertimeThresholdHours ?? 0;
                    // For casual: >threshold hours per day is overtime
                    foreach (var (date, hours) in dailyHours)
                    {
                        if (hours > casualThreshold)
                        {
                            regularHours += casualThreshold;
                            overtimeHours += hours - casualThreshold;
                        }
                        else
                        {
                            regularHours += hours;
                        }
                    }
                }
                else
                {
                    // For full-time and part-time: all hours are regular
                    regularHours = dailyHours.Values.Sum();
                }

                var regularWages = regularHours * regularRate.HourlyRate;
                var overtimeWages = overtimeHours * (overtimeRate?.HourlyRate ?? 0);
                var grossWages = regularWages + overtimeWages;

                var lineItem = new PayrollLineItem
                {
                    StaffNumber = staff.StaffNumber,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    ContractType = staff.ContractType,
                    RegularHours = regularHours,
                    OvertimeHours = overtimeHours,
                    TotalHours = regularHours + overtimeHours,
                    RegularHourlyRate = regularRate.HourlyRate,
                    OvertimeHourlyRate = overtimeRate?.HourlyRate ?? 0,
                    GrossWages = grossWages,
                    NetWages = grossWages, // No deductions for now
                    CreatedAt = DateTime.UtcNow
                };

                lineItems.Add(lineItem);
            }

            return lineItems;
        }

        private PayCalendarDto MapToDto(PayCalendar calendar)
        {
            return new PayCalendarDto
            {
                PayCalendarId = calendar.PayCalendarId,
                StartPeriodDate = calendar.StartPeriodDate,
                EndPeriodDate = calendar.EndPeriodDate,
                PayDate = calendar.PayDate,
                PayFrequency = calendar.PayFrequency,
                Status = calendar.Status,
                IsPayrollGenerated = calendar.IsPayrollGenerated,
                CreatedAt = calendar.CreatedAt,
                CreatedBy = calendar.CreatedBy
            };
        }

        private async Task<PayrollRunDto> MapToPayrollRunDto(PayrollRun run)
        {
            if (run.PayCalendar == null)
            {
                run.PayCalendar = await _payCalendarRepo.GetByIdAsync(run.PayCalendarId);
            }

            return new PayrollRunDto
            {
                PayrollRunId = run.PayrollRunId,
                PayCalendarId = run.PayCalendarId,
                TotalLabourCost = run.TotalLabourCost,
                TotalWorkHours = run.TotalWorkHours,
                StaffCount = run.StaffCount,
                Status = run.Status,
                RunNumber = run.RunNumber,
                CreatedAt = run.CreatedAt,
                CreatedBy = run.CreatedBy,
                ApprovedAt = run.ApprovedAt,
                ApprovedBy = run.ApprovedBy,
                StartPeriodDate = run.PayCalendar?.StartPeriodDate ?? DateTime.MinValue,
                EndPeriodDate = run.PayCalendar?.EndPeriodDate ?? DateTime.MinValue,
                PayDate = run.PayCalendar?.PayDate ?? DateTime.MinValue,
                LineItems = run.LineItems?.Select(li => new PayrollLineItemDto
                {
                    PayrollLineItemId = li.PayrollLineItemId,
                    PayrollRunId = li.PayrollRunId,
                    StaffNumber = li.StaffNumber,
                    FirstName = li.FirstName,
                    LastName = li.LastName,
                    ContractType = li.ContractType.ToString(),
                    RegularHours = li.RegularHours,
                    OvertimeHours = li.OvertimeHours,
                    TotalHours = li.TotalHours,
                    RegularHourlyRate = li.RegularHourlyRate,
                    OvertimeHourlyRate = li.OvertimeHourlyRate,
                    GrossWages = li.GrossWages,
                    NetWages = li.NetWages,
                    Notes = li.Notes
                }).ToList() ?? new List<PayrollLineItemDto>()
            };
        }

        #endregion
    }
}
