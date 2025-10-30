using Microsoft.AspNetCore.Mvc;
using FarmManagement.Api.ApiModels;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FarmManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly FarmManagement.Application.Services.StaffService _staffService;
        private readonly FarmManagement.Application.Repositories.IStaffRoleRepository _staffRoleRepo;
        private readonly FarmManagement.Application.Services.IIdentityService _identityService;
        private readonly FarmManagement.Application.Repositories.IDepartmentRepository _departmentRepo;

        public StaffController(FarmManagement.Application.Services.StaffService staffService,
            FarmManagement.Application.Repositories.IStaffRoleRepository staffRoleRepo,
            FarmManagement.Application.Services.IIdentityService identityService,
            FarmManagement.Application.Repositories.IDepartmentRepository departmentRepo)
        {
            _staffService = staffService;
            _staffRoleRepo = staffRoleRepo;
            _identityService = identityService;
            _departmentRepo = departmentRepo;
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            // Handle both raw JWT claims and mapped ClaimTypes
            var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var staffNumber = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? User.FindFirst(ClaimTypes.SerialNumber)?.Value;

            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                        ?? User.FindFirst(ClaimTypes.Email)?.Value;

            var roles = User.Claims
                .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct()
                .ToArray();

            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            return Ok(new { isAuthenticated, userId, email, roles });
        }





        /// <summary>Get a staff member information</summary>
        /// <param name="staffNumber">The staff number (5 digits)</param>
    /// <response code="200">Returns the staff record, including StaffRole (role name, e.g. Admin or User). AccessRole lists assigned ASP.NET identity roles (Admin/User).</response>
        /// <remarks>
        /// Sample request:
        ///     GET /api/staff/00123
        ///
        /// Sample response:
        /// {
        ///   "staffId": 1,
        ///   "staffNumber": "00123",
        ///   "firstName": "Jane",
        ///   "lastName": "Doe",
        ///   "email": "jane.doe@example.com",
        ///   "roleID": "2", // RoleID is the current role's ID
        ///   "staffRole": "Admin", // Role name
        ///   "isAdmin": true, // true if staffRole is Admin
        ///   ...
        /// }
        /// </remarks>
        /// <summary>Admin: get staff detail (includes RoleID and admin/user logic)</summary>
        [HttpGet("{staffNumber}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStaff([FromRoute] string staffNumber)
        {
            var staff = await _staffService.GetByStaffNumberAsync(staffNumber);
            if (staff == null) return NotFound();

            // Resolve current staff role and role ID (best-effort)
            int? currentRoleId = null;
            string? currentRole = null;
            try
            {
                var roles = await _staffRoleRepo.GetRolesByStaffIdAsync(staff.StaffId);
                var current = roles?.Where(r => r.IsCurrent).OrderByDescending(r => r.EffectiveFrom).FirstOrDefault()
                              ?? roles?.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();
                if (current != null)
                {
                    currentRole = current.Role?.RoleName;
                    currentRoleId = current.RoleId;
                }
            }
            catch
            {
                // swallow
            }

            var resp = new FarmManagement.Api.ApiModels.Staff.StaffView
            {
                StaffId = staff.StaffId,
                StaffNumber = staff.StaffNumber,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = staff.Email ?? string.Empty,
                StaffRole = currentRole,
                // Staff is inactive if a TerminationDate is set and it's on or before now (UTC)
                IsActive = staff.TerminationDate.HasValue && staff.TerminationDate.Value <= DateTime.UtcNow ? false : staff.IsActive,
                ContractType = staff.ContractType.ToString(),
                HireDate = staff.HireDate,
                Phone = staff.Phone,
                Address = staff.Address,
                WeeklyAvailableHour = staff.WeeklyAvailableHour
            };
            // Audit timestamps
            try
            {
                resp.CreatedAt = staff.CreatedAt;
                resp.UpdatedAt = staff.UpdatedAt;
            }
            catch { }

            // Department name (best-effort)
            try
            {
                resp.DepartmentName = staff.Department?.ToString();
                if (string.IsNullOrWhiteSpace(resp.DepartmentName) && staff.DepartmentId.HasValue)
                {
                    var did = staff.DepartmentId.Value;
                    // We only have a method to get id by name; attempt to fetch name from repository not available here.
                    // If Department navigation wasn't loaded, leave DepartmentName null.
                }
            }
            catch { }

            // Identity roles
            try
            {
                var identityRoles = await _identityService.GetRolesForStaffAsync(staff.StaffId);
                resp.AccessRole = identityRoles ?? new string[] { };
            }
            catch { }

            return Ok(resp);
        }

        /// <summary>Admin: list all staff</summary>
        /// <remarks>
        /// Query parameters (from query string): page, pageSize, departmentId, isActive, search, staffNumber, sortBy, sortDesc.
        /// Examples:
        /// GET /api/staff?staffNumber=00123
        /// GET /api/staff?search=Jane&amp;page=2
        /// </remarks>
        /// <remarks>
        /// Examples:
        /// GET /api/staff?staffNumber=00123
        /// GET /api/staff?search=Jane&amp;page=2
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? departmentId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null,
            [FromQuery] string? staffNumber = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDesc = false)
        {
            var result = await _staffService.GetAllAsync(page, pageSize, departmentId, isActive, search, staffNumber, sortBy, sortDesc);

            // Map PagedResult<Staff> to PagedResult<StaffView> and resolve current StaffRole for each staff member
            var mapped = new FarmManagement.Application.DTOs.PagedResult<FarmManagement.Api.ApiModels.Staff.StaffView>
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                Items = Enumerable.Empty<FarmManagement.Api.ApiModels.Staff.StaffView>()
            };

            var list = new List<FarmManagement.Api.ApiModels.Staff.StaffView>();
            foreach (var staff in result.Items)
            {
                string? currentRole = null;
                try
                {
                    var roles = await _staffRoleRepo.GetRolesByStaffIdAsync(staff.StaffId);
                    var current = roles?.Where(r => r.IsCurrent).OrderByDescending(r => r.EffectiveFrom).FirstOrDefault()
                                  ?? roles?.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();
                    if (current != null)
                    {
                        currentRole = current.Role?.RoleName;
                    }
                }
                catch
                {
                    // swallow - best effort
                }

                var item = new FarmManagement.Api.ApiModels.Staff.StaffView
                {
                    StaffId = staff.StaffId,
                    StaffNumber = staff.StaffNumber,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    Email = staff.Email ?? string.Empty,
                    StaffRole = currentRole,
                    // Staff is inactive if a TerminationDate is set and it's on or before now (UTC)
                    IsActive = staff.TerminationDate.HasValue && staff.TerminationDate.Value <= DateTime.UtcNow ? false : staff.IsActive,
                    ContractType = staff.ContractType.ToString(),
                    HireDate = staff.HireDate,
                    Phone = staff.Phone,
                    Address = staff.Address,
                    WeeklyAvailableHour = staff.WeeklyAvailableHour,
                    DepartmentName = staff.Department?.ToString()
                };

                // Audit timestamps
                try
                {
                    item.CreatedAt = staff.CreatedAt;
                    item.UpdatedAt = staff.UpdatedAt;
                }
                catch { }

                // Try to include AccessRole if available (best-effort)
                try
                {
                    var identityRoles = await _identityService.GetRolesForStaffAsync(staff.StaffId);
                    item.AccessRole = identityRoles ?? new string[] { };
                }
                catch { }

                list.Add(item);
            }

            mapped.Items = list;
            return Ok(mapped);
        }


        /// <summary>Admin: update staff</summary>
        [HttpPut("{staffNumber}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update([FromRoute] string staffNumber, [FromBody] FarmManagement.Application.DTOs.StaffDto req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var ok = await _staffService.UpdateByStaffNumberAsync(staffNumber, req);
                if (!ok) return NotFound();

                // Return updated staff record
                var updated = await _staffService.GetByStaffNumberAsync(staffNumber);
                if (updated == null) return NotFound();

                // Resolve role and role id
                int? currentRoleId = null;
                string? currentRole = null;
                try
                {
                    var roles = await _staffRoleRepo.GetRolesByStaffIdAsync(updated.StaffId);
                    var current = roles?.Where(r => r.IsCurrent).OrderByDescending(r => r.EffectiveFrom).FirstOrDefault()
                                  ?? roles?.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();
                    if (current != null)
                    {
                        currentRole = current.Role?.RoleName;
                        currentRoleId = current.RoleId;
                    }
                }
                catch { }

                var resp = new FarmManagement.Api.ApiModels.Staff.StaffView
                {
                    StaffId = updated.StaffId,
                    StaffNumber = updated.StaffNumber,
                    FirstName = updated.FirstName,
                    LastName = updated.LastName,
                    Email = updated.Email ?? string.Empty,
                    StaffRole = currentRole,
                    IsActive = updated.TerminationDate.HasValue && updated.TerminationDate.Value <= DateTime.UtcNow ? false : updated.IsActive,
                    TerminationDate = updated.TerminationDate,
                    ContractType = updated.ContractType.ToString(),
                    HireDate = updated.HireDate,
                    Phone = updated.Phone,
                    Address = updated.Address,
                    WeeklyAvailableHour = updated.WeeklyAvailableHour
                };

                // Audit timestamps
                try
                {
                    resp.CreatedAt = updated.CreatedAt;
                    resp.UpdatedAt = updated.UpdatedAt;
                }
                catch { }

                try
                {
                    var identityRoles = await _identityService.GetRolesForStaffAsync(updated.StaffId);
                    resp.AccessRole = identityRoles ?? new string[] { };
                }
                catch { }

                return Ok(resp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Admin: delete staff</summary>
        [HttpDelete("{staffNumber}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] string staffNumber)
        {
            var ok = await _staffService.DeleteByStaffNumberAsync(staffNumber);
            if (!ok) return NotFound(new { message = $"Staff with number '{staffNumber}' not found." });

            return Ok(new { message = "Staff deleted successfully", staffNumber });
        }

        /*
        <summary>Edit a staff member information</summary>
        <param name="staffId">The staff ID</param>
        <param name="req">The patch request content</param>
        <response code="200">Returns updated message</response>
        <remarks>
        Sample request:
            PATCH /api/staff/12345
        </remarks>
        */
        /*[HttpPatch("{staffId}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public ActionResult<string> ModifyStaff([FromRoute] int staffId, [FromBody] StaffPatchRequest req)
        {
            string message = "";
            if (req.FirstName != null)
            {
                message += $"new firstname: {req.FirstName}. ";
            }
            if (req.LastName != null)
            {
                message += $"new lastname: {req.LastName}.";
            }
            if (message == "")
            {
                message = "no changes";
            }
            return Ok($"ID: {staffId} {message}");
        } 
        */

        /// <summary>Create a new staff member (Admin only)</summary>
    /// <param name="req">The staff details. Front-end should send `staffRole` (job role name). `StaffRoleId` will be resolved server-side and is ignored on create.</param>
        /// <response code="201">Returns the created staff record, including StaffRole and AccessRole. AccessRole contains assigned ASP.NET identity roles (Admin/User).</response>
        /// <response code="400">If the request is invalid or creation fails</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not an admin</response>
        /// <remarks>
        /// Sample request (required fields only):
        ///     POST /api/staff/create-staff
        ///     {
        ///         "firstName": "Jane",
        ///         "lastName": "Doe",
        ///         "email": "jane.doe@example.com",
        ///         "staffNumber": "00123",
        ///         "contractType": "FullTime",
        ///         "staffRole": "Picker",
        ///         "accessRole": ["User"],
        ///         "sendTempPasswordEmail": true
        ///     }
        /// </remarks>
        [HttpPost("create-staff")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(FarmManagement.Api.ApiModels.Staff.StaffView), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<FarmManagement.Api.ApiModels.Staff.StaffView>> CreateStaff([FromBody] FarmManagement.Application.DTOs.StaffDto req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var created = await _staffService.CreateAsync(req);

                // Build response using server-resolved role mappings
                string? serverRoleName = null;
                string roleIdString = string.Empty;
                try
                {
                    var roles = await _staffRoleRepo.GetRolesByStaffIdAsync(created.StaffId);
                    var current = roles?.Where(r => r.IsCurrent).OrderByDescending(r => r.EffectiveFrom).FirstOrDefault()
                                  ?? roles?.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();
                    if (current != null)
                    {
                        serverRoleName = current.Role?.RoleName;
                        roleIdString = current.RoleId.ToString();
                    }
                }
                catch { }

                var staff = new FarmManagement.Api.ApiModels.Staff.StaffView
                {
                    StaffId = created.StaffId,
                    StaffNumber = created.StaffNumber,
                    FirstName = created.FirstName,
                    LastName = created.LastName,
                    Email = created.Email ?? string.Empty,
                    StaffRole = serverRoleName,
                    IsActive = created.TerminationDate.HasValue && created.TerminationDate.Value <= DateTime.UtcNow ? false : created.IsActive,
                    ContractType = req.ContractType,
                    HireDate = created.HireDate,
                    Phone = created.Phone,
                    Address = created.Address,
                    WeeklyAvailableHour = created.WeeklyAvailableHour
                };

                // Audit timestamps
                try
                {
                    staff.CreatedAt = created.CreatedAt;
                    staff.UpdatedAt = created.UpdatedAt;
                }
                catch { }

                // Fill department name if possible
                try
                {
                    if (created.DepartmentId.HasValue)
                    {
                        // We don't have a direct method to fetch department by id here; if Department navigation is loaded use it
                        staff.DepartmentName = created.Department?.ToString();
                    }
                }
                catch { }

                // Get identity roles for the created staff
                try
                {
                    var identityRoles = await _identityService.GetRolesForStaffAsync(created.StaffId);
                    staff.AccessRole = identityRoles ?? new string[] { };
                }
                catch { }

                return CreatedAtAction(nameof(GetStaff), new { staffNumber = staff.StaffNumber }, staff);
            }
            catch (Exception ex)
            {
                // Log the error
                return BadRequest(new { message = "Failed to create staff member", error = ex.Message });
            }
        }

        /*
        Update sample:
            PUT /api/staff/00123
            {
                "firstName": "Janet",
                "lastName": "Doe",
                "email": "janet.doe@example.com",
                "staffNumber": "00123",             // Required by model; should match the route but is not changed
                "contractType": "PartTime",            // Allowed: Casual | PartTime | FullTime
                "staffRole": "Admin",
                "accessRole": ["Admin"],               // Identity access roles can be replaced on update
                "hireDate": "2025-09-07",
                "phone": "0411222333",
                "address": "456 Side St",
                "weeklyAvailableHour": 38,
                "terminationDate": "2026-01-31"      // TerminationDate is allowed only on updates
            }
        */
    }

}