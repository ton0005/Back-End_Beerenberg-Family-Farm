using FarmManagement.Application.DTOs;
using FarmManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;


namespace FarmManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeEntriesController : ControllerBase
    {
        public class ResolveExceptionRequest
        {
            public string ResolvedBy { get; set; } = string.Empty;
            public string ResolutionNotes { get; set; } = string.Empty;
        }
    private readonly ITimeEntryService _service;
    private readonly FarmManagement.Application.Services.StaffService _staffService;
    private readonly FarmManagement.Application.Repositories.IAuditRepository _auditRepo;

        private bool IsAdmin => User?.IsInRole("Admin") ?? false;
        private string CurrentStaffNumber => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        private async Task<string?> ResolveCurrentStaffNumberAsync()
        {
            // Try common claims first
            var sn = User?.FindFirst("staffNumber")?.Value
                     ?? User?.FindFirst(ClaimTypes.SerialNumber)?.Value
                     ?? User?.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrWhiteSpace(sn)) return sn;

            // Next: prefer explicit numeric staffId claim if present
            var idStr = User?.FindFirst("staffId")?.Value
                        ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User?.FindFirst("uid")?.Value
                        ?? User?.FindFirst("sub")?.Value;
            if (int.TryParse(idStr, out var staffId))
            {
                try
                {
                    var staff = await _staffService.GetByIdAsync(staffId);
                    return staff?.StaffNumber;
                }
                catch { }
            }
            return null;
        }

        public TimeEntriesController(ITimeEntryService service, FarmManagement.Application.Services.StaffService staffService, FarmManagement.Application.Repositories.IAuditRepository auditRepo)
        {
            _service = service;
            _staffService = staffService;
            _auditRepo = auditRepo;
        }

        [HttpPost("clock")]
        [AllowAnonymous]
        public async Task<IActionResult> Clock([FromBody] TimeEntryDto req)
        {
            if (string.IsNullOrWhiteSpace(req.StaffNumber)) return BadRequest("StaffNumber is required");

            var isAuthenticated = User?.Identity?.IsAuthenticated == true;

            // If authenticated and not admin, enforce self clocking rule
            if (isAuthenticated && !IsAdmin && !string.Equals(req.StaffNumber, CurrentStaffNumber, System.StringComparison.OrdinalIgnoreCase))
                return Forbid();

            // Bypass shift validation only allowed for authenticated admins
            if (req.BypassShiftValidation == true && (!isAuthenticated || !IsAdmin))
                return Forbid();

            // Set ModifiedBy: if authenticated use current staff/admin; else fallback to staffNumber (kiosk mode)
            if (isAuthenticated)
                req.ModifiedBy = CurrentStaffNumber;
            else
                req.ModifiedBy = req.StaffNumber; // anonymous kiosk entry

            var created = await _service.ClockAsync(req);
            return Ok(created);
        }

        [HttpGet("staff/{staffNumber}")]
        public async Task<IActionResult> GetForStaff([FromRoute] string staffNumber)
        {
            // Staff can view only their own records; admin can view any
            if (!IsAdmin && !string.Equals(staffNumber, CurrentStaffNumber, System.StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var items = await _service.GetByStaffNumberAsync(staffNumber);
            return Ok(items);
        }

        [HttpGet("staff/{staffNumber}/today")]
        public async Task<IActionResult> GetToday([FromRoute] string staffNumber)
        {
            if (!IsAdmin && !string.Equals(staffNumber, CurrentStaffNumber, System.StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var items = await _service.GetTodayEntriesAsync(staffNumber);
            return Ok(items);
        }

        [HttpPut("{entryId}/manual")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManualEdit([FromRoute] int entryId, [FromBody] TimeEntryDto req)
        {
            try
            {
                // Ensure audit fields are populated: ModifiedBy should be the admin performing the edit
                // and ModifiedReason must be provided to justify the change.
                if (string.IsNullOrWhiteSpace(req.ModifiedReason))
                    return BadRequest("ModifiedReason is required for manual edits");

                var adminStaffNumber = await ResolveCurrentStaffNumberAsync();
                if (string.IsNullOrWhiteSpace(adminStaffNumber))
                {
                    // If we cannot resolve a staffNumber, fail explicitly to avoid incorrect auditing
                    return BadRequest("Unable to resolve current admin staffNumber for auditing");
                }
                req.ModifiedBy = adminStaffNumber;
                if (req.ModifiedAt == null)
                    req.ModifiedAt = System.DateTime.UtcNow;

                var updated = await _service.ManualEditAsync(entryId, req);
                return Ok(updated);
            }
            catch (System.ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("exceptions")]
        public async Task<IActionResult> CreateException([FromBody] ExceptionDto req)
        {
            // Staff can create their own exceptions; admins can create for any staff
            if (!IsAdmin && !string.Equals(req.StaffNumber, CurrentStaffNumber, System.StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var created = await _service.CreateExceptionAsync(req);
            return Ok(created);
        }

        [HttpGet("staff/{staffNumber}/exceptions/{date}")]
        public async Task<IActionResult> GetExceptions([FromRoute] string staffNumber, [FromRoute] System.DateOnly date)
        {
            if (!IsAdmin && !string.Equals(staffNumber, CurrentStaffNumber, System.StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var list = await _service.GetExceptionsAsync(staffNumber, date);
            return Ok(list);
        }

        [HttpPost("exceptions/{id}/resolve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResolveException([FromRoute] int id, [FromBody] ResolveExceptionRequest req)
        {
            try
            {
                var updated = await _service.ResolveExceptionAsync(id, req.ResolvedBy, req.ResolutionNotes);
                return Ok(updated);
            }
            catch (System.ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // Admin: edit a full session for a staff and date (clock in/out + multiple breaks) in one request
        [HttpPut("staff/{staffNumber}/sessions/{date}/manual")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManualEditSession([FromRoute] string staffNumber, [FromRoute] System.DateOnly date, [FromBody] ManualSessionEditRequest req)
        {
            if (req == null) return BadRequest("Request body is required");
            if (string.IsNullOrWhiteSpace(req.ModifiedReason)) return BadRequest("ModifiedReason is required");

            var adminStaffNumber = await ResolveCurrentStaffNumberAsync();
            if (string.IsNullOrWhiteSpace(adminStaffNumber))
                return BadRequest("Unable to resolve current admin staffNumber for auditing");

            req.ModifiedBy = adminStaffNumber;
            if (req.ModifiedAt == null) req.ModifiedAt = System.DateTime.UtcNow;

            var result = await _service.ManualEditSessionAsync(staffNumber, date, req, adminStaffNumber);
            // Enrich with staff name
            var staff = await _staffService.GetByStaffNumberAsync(staffNumber);
            result.FirstName = staff?.FirstName;
            result.LastName = staff?.LastName;

            return Ok(result);
        }

        // Admin only: query across staff with optional filters
        [HttpGet("query")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Query([FromQuery] string? staffNumber = null, [FromQuery] int? entryTypeId = null, [FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var items = await _service.QueryAsync(staffNumber, entryTypeId, start, end, page, pageSize);
            return Ok(items);
        }

        // Summarize a staff session for a given date (clock in -> break start -> break end -> clock out)
        // If date not provided, default to today. Non-admins can only view their own session.
        [HttpGet("staff/{staffNumber}/sessions")]
        public async Task<IActionResult> GetStaffSessions(
            [FromRoute] string staffNumber,
            [FromQuery] DateOnly? date = null,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null)
        {
            if (!IsAdmin && !string.Equals(staffNumber, CurrentStaffNumber, System.StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var sessions = await _service.GetSessionsAsync(staffNumber, date, startDate, endDate);

            // Enrich with staff name
            var staff = await _staffService.GetByStaffNumberAsync(staffNumber);
            var first = staff?.FirstName;
            var last = staff?.LastName;

            var enriched = sessions.Select(s => {
                var dto = new FarmManagement.Application.DTOs.StaffSessionDto
                {
                    StaffNumber = s.StaffNumber,
                    FirstName = first,
                    LastName = last,
                    Date = s.Date,
                    ClockIn = s.ClockIn,
                    BreakStart = s.BreakStart,
                    BreakEnd = s.BreakEnd,
                    ClockOut = s.ClockOut,
                    Breaks = s.Breaks?.ToList() ?? new List<FarmManagement.Application.DTOs.BreakIntervalDto>(),
                    TotalBreakMinutes = s.TotalBreakMinutes,
                    WorkedMinutes = s.WorkedMinutes
                };
                // Ensure back-compat fields populated from Breaks if missing
                if (dto.BreakStart == null && dto.Breaks.Count > 0)
                {
                    dto.BreakStart = dto.Breaks[0].Start;
                    dto.BreakEnd = dto.Breaks[0].End;
                }
                return dto;
            });

            return Ok(enriched);
        }

        // Admin: get audits for a single time entry (used by frontend toggle)
        [HttpGet("{entryId}/audits")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAuditsForEntry([FromRoute] int entryId, [FromQuery] bool fullChanges = false)
        {
            var audits = await _auditRepo.GetByTableAndRecordIdsAsync("TimeEntries", new[] { entryId });
            var dto = audits.Select(a => new FarmManagement.Application.DTOs.AuditDto
            {
                AuditId = a.AuditId,
                TableName = a.TableName,
                RecordId = a.RecordId,
                ActionType = a.ActionType,
                ChangesJson = fullChanges ? a.ChangesJson : (a.ChangesJson == null ? null : (a.ChangesJson.Length > 200 ? a.ChangesJson.Substring(0, 200) + "..." : a.ChangesJson)),
                PerformedBy = a.PerformedBy,
                PerformedAt = a.PerformedAt
            }).ToArray();

            return Ok(dto);
        }

        // Admin: list sessions for all staff over a date or range
        [HttpGet("sessions")] 
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllSessions([FromQuery] DateOnly? date = null, [FromQuery] DateOnly? startDate = null, [FromQuery] DateOnly? endDate = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var paged = await _service.GetAllStaffSessionsAsync(date, startDate, endDate, page, pageSize);

            // Enrich with names within the page only to keep cost bounded
            var staffNumbers = paged.Items.Select(s => s.StaffNumber).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var nameMap = new Dictionary<string, (string? first, string? last)>(StringComparer.OrdinalIgnoreCase);
            foreach (var sn in staffNumbers)
            {
                var staff = await _staffService.GetByStaffNumberAsync(sn);
                nameMap[sn] = (staff?.FirstName, staff?.LastName);
            }

            var enrichedItems = paged.Items.Select(s => {
                var dto = new FarmManagement.Application.DTOs.StaffSessionDto
                {
                    StaffNumber = s.StaffNumber,
                    FirstName = nameMap.TryGetValue(s.StaffNumber, out var n) ? n.first : null,
                    LastName = nameMap.TryGetValue(s.StaffNumber, out var n2) ? n2.last : null,
                    Date = s.Date,
                    ClockIn = s.ClockIn,
                    BreakStart = s.BreakStart,
                    BreakEnd = s.BreakEnd,
                    ClockOut = s.ClockOut,
                    Breaks = s.Breaks?.ToList() ?? new List<FarmManagement.Application.DTOs.BreakIntervalDto>(),
                    TotalBreakMinutes = s.TotalBreakMinutes,
                    WorkedMinutes = s.WorkedMinutes
                };
                if (dto.BreakStart == null && dto.Breaks.Count > 0)
                {
                    dto.BreakStart = dto.Breaks[0].Start;
                    dto.BreakEnd = dto.Breaks[0].End;
                }
                return dto;
            }).OrderBy(e => e.StaffNumber).ThenBy(e => e.Date).ToList();

            var response = new FarmManagement.Application.DTOs.PagedResult<FarmManagement.Application.DTOs.StaffSessionDto>
            {
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                Items = enrichedItems
            };

            return Ok(response);
        }
    }
}
