using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Threading;
using FarmManagement.Application.Services;
using FarmManagement.Application.DTOs;

namespace FarmManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftsController : ControllerBase
    {
        private readonly IShiftService _shiftService;

        public ShiftsController(IShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        // Admin: list shift types (lookup)
        [HttpGet("types")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTypes()
        {
            var types = await _shiftService.GetShiftTypesAsync();
            return Ok(types);
        }

        // List shifts with paging & filters
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? shiftTypeId = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDesc = false,
            [FromQuery] bool onlyPublished = false,
            CancellationToken ct = default)
        {
            var result = await _shiftService.GetAllShiftsAsync(page, pageSize, startDate, endDate, shiftTypeId, search, sortBy, sortDesc, onlyPublished, ct);
            return Ok(result);
        }

        // Admin: create shift
        /// <summary>
        /// Create a new shift. For named templates (e.g. "Morning") you can pass only the template name
        /// and date; the server will apply default start/end times. For custom shifts you must supply start and end times.
        /// </summary>
        /// <example>
        /// Template-based creation (Morning) - JSON:
        /// <![CDATA[
        /// {
        ///   "shiftTypeName": "Morning",
        ///   "date": "2025-10-01",
        ///   "note": "Barn cleanup",
        ///   "isPublished": true
        /// }
        /// ]]>
        /// </example>
        /// <example>
        /// Custom shift with explicit times - JSON:
        /// <![CDATA[
        /// {
        ///   "shiftTypeName": "Custom",
        ///   "date": "2025-10-01",
        ///   "startTime": "14:00:00",
        ///   "endTime": "18:30:00",
        ///   "note": "Special harvest",
        ///   "isPublished": false
        /// }
        /// ]]>
        /// </example>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateShift([FromBody] ApiModels.Shifts.ShiftCreateRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var dto = new ShiftDto
            {
                ShiftTypeName = req.ShiftTypeName,
                Date = req.Date,
                Assignments = req.StaffNumbers.Select(x => new ShiftAssignmentDto
                {
                    StaffNumber = x
                }).ToArray()
            };

            try
            {
                var (shiftDto, error) = await _shiftService.CreateShiftAsync(dto);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    return BadRequest(error);
                }
                
                return CreatedAtAction(nameof(GetShift), new { id = shiftDto?.ShiftId }, shiftDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Get shift by id - Admin can view any shift, normal user can view only assigned shifts
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetShift([FromRoute] int id)
        {
            var isAdmin = User.IsInRole("Admin");
            if (isAdmin)
            {
                var shift = await _shiftService.GetShiftByIdAsync(id);
                if (shift == null) return NotFound();

                // For admins include assignments for this shift
                var shiftAssignments = (await _shiftService.GetAssignmentsByShiftIdAsync(id)).Select(a => new FarmManagement.Api.ApiModels.Shifts.ShiftAssignmentView
                {
                    ShiftAssignmentId = a.ShiftAssignmentId,
                    ShiftId = a.ShiftId,
                    StaffNumber = a.StaffNumber ?? string.Empty,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    RoleName = a.RoleName,
                    Status = a.Status,
                    AssignedAt = a.AssignedAt,
                    CompletedAt = a.CompletedAt
                });

                var view = new FarmManagement.Api.ApiModels.Shifts.ShiftView
                {
                    ShiftId = shift.ShiftId,
                    ShiftTypeId = shift.ShiftTypeId,
                    Date = shift.Date,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    Break = shift.Break,
                    Note = shift.Note,
                    IsPublished = shift.IsPublished,
                    Assignments = shiftAssignments
                };

                return Ok(view);
            }

            var staffIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(staffIdClaim)) return Forbid();
            if (!int.TryParse(staffIdClaim, out var staffId)) return Forbid();

            var assignments = await _shiftService.GetAssignedShiftsForStaffAsync(staffId);
            var hasAssignment = assignments.Any(a => a.ShiftId == id);
            if (!hasAssignment) return Forbid();

            var shiftDto = await _shiftService.GetShiftByIdAsync(id);
            if (shiftDto == null) return NotFound();
            return Ok(shiftDto);
        }

        // List assignments by staffNumber
        [HttpGet("assignments/{staffNumber}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAssignmentsByStaffNumber([FromRoute] string staffNumber, CancellationToken ct)
        {
            var items = await _shiftService.GetAssignmentsByStaffNumberAsync(staffNumber, ct);
            // pre-load shift types to resolve names
            var shiftTypes = (await _shiftService.GetShiftTypesAsync()).ToList();

            var shaped = new List<FarmManagement.Api.ApiModels.Shifts.ShiftAssignmentView>();
            foreach (var a in items)
            {
                var shift = await _shiftService.GetShiftByIdAsync(a.ShiftId);
                string? shiftTypeName = null;
                DateTime date = DateTime.MinValue;
                TimeSpan? start = null;
                TimeSpan? end = null;
                if (shift != null)
                {
                    shiftTypeName = shiftTypes.FirstOrDefault(t => t.ShiftTypeId == shift.ShiftTypeId)?.Name;
                    date = shift.Date;
                    start = shift.StartTime;
                    end = shift.EndTime;
                }

                shaped.Add(new FarmManagement.Api.ApiModels.Shifts.ShiftAssignmentView
                {
                    ShiftAssignmentId = a.ShiftAssignmentId,
                    ShiftId = a.ShiftId,
                    StaffNumber = a.StaffNumber ?? staffNumber,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    RoleName = a.RoleName,
                    ShiftTypeName = shiftTypeName,
                    Date = date,
                    StartTime = start,
                    EndTime = end,
                    Status = a.Status,
                    AssignedAt = a.AssignedAt,
                    CompletedAt = a.CompletedAt
                });
            }

            return Ok(shaped);
        }

        // Admin: delete shift
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var ok = await _shiftService.DeleteShiftAsync(id);
            if (!ok) return NotFound();
            return Ok(new { message = "Deleted" });
        }

        // Admin: delete a single assignment by assignment id
        [HttpDelete("assignments/{assignmentId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAssignment([FromRoute] int assignmentId)
        {
            var ok = await _shiftService.DeleteAssignmentAsync(assignmentId);
            if (!ok) return NotFound(new { message = "Assignment not found" });
            return Ok(new { message = "Assignment deleted" });
        }
    }
}
