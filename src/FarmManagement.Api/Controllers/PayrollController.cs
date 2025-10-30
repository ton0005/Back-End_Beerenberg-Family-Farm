using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FarmManagement.Application.Services;
using FarmManagement.Application.DTOs;
using System.Security.Claims;

namespace FarmManagement.Api.Controllers
{
    /// <summary>
    /// Manages payroll operations including pay calendars and payroll runs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _payrollService;
        private readonly ILogger<PayrollController> _logger;

        public PayrollController(
            IPayrollService payrollService,
            ILogger<PayrollController> logger)
        {
            _payrollService = payrollService;
            _logger = logger;
        }

        #region Pay Calendar Endpoints

        /// <summary>
        /// Create a new pay calendar standard (fortnightly)
        /// </summary>
        /// <remarks>
        /// Creates a fortnightly pay calendar. The end period date is auto-generated as start date + 13 days.
        /// Pay date must be after the end period date.
        /// 
        /// Sample request:
        ///     POST /api/payroll/calendar
        ///     {
        ///       "startPeriodDate": "2025-10-20",
        ///       "payDate": "2025-11-05"
        ///     }
        /// </remarks>
        /// <param name="request">Pay calendar creation request</param>
        /// <returns>Created pay calendar</returns>
        /// <response code="200">Pay calendar created successfully</response>
        /// <response code="400">Invalid request (overlapping calendar, invalid dates, etc.)</response>
        [HttpPost("calendar")]
        [ProducesResponseType(typeof(PayCalendarDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePayCalendar([FromBody] CreatePayCalendarRequest request)
        {
            try
            {
                var createdBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var calendar = await _payrollService.CreatePayCalendarAsync(request, createdBy);
                return Ok(calendar);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Failed to create pay calendar: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get all pay calendars
        /// </summary>
        /// <remarks>
        /// Returns all pay calendar standards ordered by start date (newest first).
        /// 
        /// Sample request:
        ///     GET /api/payroll/calendar
        /// </remarks>
        /// <returns>List of pay calendars</returns>
        /// <response code="200">Returns list of pay calendars (empty array if none exist)</response>
        [HttpGet("calendar")]
        [ProducesResponseType(typeof(List<PayCalendarListResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPayCalendars()
        {
            var calendars = await _payrollService.GetAllPayCalendarsAsync();
            return Ok(calendars);
        }

        /// <summary>
        /// Get a specific pay calendar by ID
        /// </summary>
        /// <param name="id">Pay calendar ID</param>
        /// <returns>Pay calendar details</returns>
        /// <response code="200">Returns the pay calendar</response>
        /// <response code="404">Pay calendar not found</response>
        [HttpGet("calendar/{id}")]
        [ProducesResponseType(typeof(PayCalendarDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPayCalendarById(int id)
        {
            var calendar = await _payrollService.GetPayCalendarByIdAsync(id);
            if (calendar == null)
            {
                return NotFound(new { message = "Pay calendar not found" });
            }
            return Ok(calendar);
        }

        #endregion

        #region Payroll Run Endpoints

        /// <summary>
        /// Create/regenerate payroll for a pay period
        /// </summary>
        /// <remarks>
        /// Calculates payroll for all active staff for the specified pay calendar period.
        /// 
        /// Rules:
        /// - End period date must have passed
        /// - If payroll already exists, it will be recalculated
    /// - Full-time/Part-time: use configured regular hourly rate
    /// - Casual: use configured casual regular and overtime rates (overtime rules apply per configuration)
        /// 
        /// Sample request:
        ///     POST /api/payroll/create
        ///     {
        ///       "payCalendarId": 1
        ///     }
        /// </remarks>
        /// <param name="request">Payroll creation request</param>
        /// <returns>Created payroll run with line items</returns>
        /// <response code="200">Payroll created successfully</response>
        /// <response code="400">Pay period not completed or calendar not found</response>
        [HttpPost("create")]
        [ProducesResponseType(typeof(PayrollRunDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePayroll([FromBody] CreatePayrollRequest request)
        {
            try
            {
                var createdBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var payrollRun = await _payrollService.CreatePayrollAsync(request.PayCalendarId, createdBy);
                
                _logger.LogInformation(
                    "Payroll created: PayrollRunId={PayrollRunId}, TotalCost={TotalCost}, StaffCount={StaffCount}",
                    payrollRun.PayrollRunId, payrollRun.TotalLabourCost, payrollRun.StaffCount);

                return Ok(payrollRun);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Failed to create payroll: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get detailed payroll run by ID
        /// </summary>
        /// <param name="id">Payroll run ID</param>
        /// <returns>Payroll run with all line items</returns>
        /// <response code="200">Returns the payroll run</response>
        /// <response code="404">Payroll run not found</response>
        [HttpGet("run/{id}")]
        [ProducesResponseType(typeof(PayrollRunDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPayrollRunById(int id)
        {
            var payrollRun = await _payrollService.GetPayrollRunByIdAsync(id);
            if (payrollRun == null)
            {
                return NotFound(new { message = "Payroll run not found" });
            }
            return Ok(payrollRun);
        }

        /// <summary>
        /// Get the latest payroll run for a specific pay calendar
        /// </summary>
        /// <param name="calendarId">Pay calendar ID</param>
        /// <returns>Latest payroll run with all line items</returns>
        /// <response code="200">Returns the latest payroll run for the calendar</response>
        /// <response code="404">Payroll run not found for this calendar</response>
        [HttpGet("run/calendar/{calendarId}")]
        [ProducesResponseType(typeof(PayrollRunDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPayrollRunByCalendarId(int calendarId)
        {
            var payrollRuns = await _payrollService.GetPayrollHistoryByCalendarAsync(calendarId);
            
            if (!payrollRuns.Any())
            {
                return NotFound(new { message = "No payroll run found for this calendar" });
            }

            // Get the latest run (most recent by created date)
            var latestRun = payrollRuns.OrderByDescending(r => r.CreatedAt).First();
            var payrollRunDetails = await _payrollService.GetPayrollRunByIdAsync(latestRun.PayrollRunId);
            
            if (payrollRunDetails == null)
            {
                return NotFound(new { message = "Payroll run not found" });
            }
            
            return Ok(payrollRunDetails);
        }

        /// <summary>
        /// Get all payroll history
        /// </summary>
        /// <remarks>
        /// Returns summary of all payroll runs across all pay periods.
        /// 
        /// Sample request:
        ///     GET /api/payroll/history
        /// </remarks>
        /// <returns>List of payroll summaries</returns>
        /// <response code="200">Returns list of payroll summaries (empty array if none exist)</response>
        [HttpGet("history")]
        [ProducesResponseType(typeof(List<PayrollSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPayrollHistory()
        {
            var history = await _payrollService.GetAllPayrollHistoryAsync();
            
            if (!history.Any())
            {
                return Ok(new { message = "No Available Payroll History", data = new List<PayrollSummaryDto>() });
            }

            return Ok(history);
        }

        /// <summary>
        /// Get payroll history for a specific pay calendar
        /// </summary>
        /// <param name="calendarId">Pay calendar ID</param>
        /// <returns>List of payroll runs for the calendar</returns>
        /// <response code="200">Returns list of payroll summaries</response>
        [HttpGet("history/calendar/{calendarId}")]
        [ProducesResponseType(typeof(List<PayrollSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPayrollHistoryByCalendar(int calendarId)
        {
            var history = await _payrollService.GetPayrollHistoryByCalendarAsync(calendarId);
            return Ok(history);
        }

        #endregion

        #region Pay Rates Endpoints

        /// <summary>
        /// Get all pay rates
        /// </summary>
        /// <remarks>
        /// Returns all configured pay rates including historical rates.
        /// 
    /// Current rates are data-driven and stored in the PayRates table. Use the PayRates endpoints to view configured rates.
        /// </remarks>
        /// <returns>List of pay rates</returns>
        /// <response code="200">Returns list of pay rates</response>
        [HttpGet("rates")]
        [ProducesResponseType(typeof(List<PayRateDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPayRates()
        {
            var rates = await _payrollService.GetAllPayRatesAsync();
            return Ok(rates);
        }

        #endregion
    }
}
