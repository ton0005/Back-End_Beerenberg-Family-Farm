using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading;
using FarmManagement.Application.Services;
using System.Security.Claims;
using System.Linq;
using FarmManagement.Application.DTOs;

namespace FarmManagement.Api.Controllers
{
    [ApiController]
    [Route("api/public/shifts")]
    public class PublicShiftsController : ControllerBase
    {
        private readonly IShiftService _shiftService;
        public PublicShiftsController(IShiftService shiftService) { _shiftService = shiftService; }

        // Public (staff) endpoint - returns only published shifts assigned to the authenticated staff
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(PagedResult<PublicShiftDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] System.DateTime? startDate = null,
            [FromQuery] System.DateTime? endDate = null,
            [FromQuery] int? shiftTypeId = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDesc = false,
            CancellationToken ct = default)
        {
            // Resolve staff id from several possible claim types (NameIdentifier, uid, sub)
            // Prefer explicit numeric staffId claim
            string? staffIdRaw = User.FindFirst("staffId")?.Value
                                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("uid")?.Value
                                  ?? User.FindFirst("sub")?.Value;

            int staffId = 0;
            if (string.IsNullOrWhiteSpace(staffIdRaw) || !int.TryParse(staffIdRaw, out staffId))
            {
                // If we only have a GUID identity user id, try to resolve via DI (UserManager) to fetch StaffId
                // This adds a minor per-request overhead; acceptable fallback.
                var services = HttpContext.RequestServices;
                try
                {
                    var userManager = services.GetService<Microsoft.AspNetCore.Identity.UserManager<FarmManagement.Core.Entities.Identity.ApplicationUser>>();
                    var email = User.FindFirst(ClaimTypes.Email)?.Value;
                    Microsoft.AspNetCore.Identity.UserManager<FarmManagement.Core.Entities.Identity.ApplicationUser>? um = userManager;
                    FarmManagement.Core.Entities.Identity.ApplicationUser? user = null;
                    if (um != null)
                    {
                        // Try by NameIdentifier (could be GUID) first
                        var nameId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrWhiteSpace(nameId))
                        {
                            user = um.FindByIdAsync(nameId).GetAwaiter().GetResult();
                        }
                        if (user == null && !string.IsNullOrWhiteSpace(email))
                        {
                            user = um.FindByEmailAsync(email).GetAwaiter().GetResult();
                        }
                        if (user != null && user.StaffId > 0)
                        {
                            staffId = user.StaffId;
                        }
                    }
                }
                catch { /* ignore */ }

                if (staffId <= 0)
                {
                    var available = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                    return Unauthorized(new { message = "StaffId claim missing or invalid after fallback resolution", availableClaims = available });
                }
            }
            // Use server-side filtering/paging via service (returns typed PagedResult<PublicShiftDto>)
            var result = await _shiftService.GetPublicShiftsForStaffAsync(
                staffId,
                page,
                pageSize,
                startDate,
                endDate,
                shiftTypeId,
                search,
                sortBy,
                sortDesc,
                ct);

            return Ok(result);
        }
    }
}
