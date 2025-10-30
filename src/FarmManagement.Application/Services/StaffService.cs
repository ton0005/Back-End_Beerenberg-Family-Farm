
using FarmManagement.Application.DTOs;
using FarmManagement.Core.Entities;
using FarmManagement.Application.Repositories;
using FarmManagement.Application.Security;
using FarmManagement.Application.Services;

namespace FarmManagement.Application.Services;

public class StaffService
{
    private readonly IStaffRepository _repo;
    private readonly IAuthUserRepository _authRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _emailService;
    private readonly IStaffRoleRepository _staffRoleRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IIdentityService _identityService;

    public StaffService(IStaffRepository repo, IAuthUserRepository authRepo, IPasswordHasher hasher, IEmailService emailService, IStaffRoleRepository staffRoleRepo, IPasswordResetService passwordResetService, IIdentityService identityService, IDepartmentRepository departmentRepo)
    {
        _repo = repo;
        _authRepo = authRepo;
        _hasher = hasher;
        _emailService = emailService;
        _staffRoleRepo = staffRoleRepo;
        _passwordResetService = passwordResetService;
        _identityService = identityService;
        _departmentRepo = departmentRepo;
    }

    public async Task<PagedResult<Staff>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        int? departmentId = null,
        bool? isActive = null,
        string? search = null,
        string? staffNumber = null,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken ct = default)
        => await _repo.GetAllAsync(page, pageSize, departmentId, isActive, search, staffNumber, sortBy, sortDesc, ct);

    public async Task<Staff?> GetByIdAsync(int staffId, CancellationToken ct = default)
        => await _repo.GetByIdAsync(staffId, ct);

    public async Task<Staff?> GetByStaffNumberAsync(string staffNumber, CancellationToken ct = default)
        => await _repo.GetByStaffNumberAsync(staffNumber, ct);

    public async Task<Staff> CreateAsync(StaffDto dto, CancellationToken ct = default)
    {
        // Resolve department id if name provided and id not supplied
        int? resolvedDeptId = dto.DepartmentId;
        if (!resolvedDeptId.HasValue && !string.IsNullOrWhiteSpace(dto.DepartmentName))
        {
            var id = await _departmentRepo.GetDepartmentIdByNameAsync(dto.DepartmentName!, ct);
            if (!id.HasValue)
            {
                throw new ArgumentException($"Department with name '{dto.DepartmentName}' not found.");
            }
            resolvedDeptId = id.Value;
        }

        var staff = new Staff
        {
            StaffNumber = dto.StaffNumber,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            // Parse ContractType safely (case-insensitive) and provide a clear error on invalid value
            ContractType = Enum.TryParse<Core.Enums.ContractTypeEnum>(dto.ContractType, true, out var parsedCreateCt)
                ? parsedCreateCt
                : throw new ArgumentException($"Invalid contract type '{dto.ContractType}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(Core.Enums.ContractTypeEnum)))}"),
            // Resolve department: prefer explicit DepartmentId; otherwise try DepartmentName -> id
            DepartmentId = resolvedDeptId,
            WeeklyAvailableHour = dto.WeeklyAvailableHour,
            HireDate = dto.HireDate,
            // TerminationDate is intentionally NOT set on create. Termination should only be applied via update/edit.
            TerminationDate = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(staff, ct);

    // Determine staff role mapping from dto.StaffRole (preferred). Front-end should send the job role name.
    // We still accept StaffRoleId for backward-compat. Resolve to an internal RoleId and the job role name
    // (from AppRoles). We then map that job role to an ASP.NET Identity role (only "Admin" or "User")
    // when creating the Identity user so AspNetUserRoles remain limited to the identity role set while
    // job roles are stored separately in AppRoles/StaffRoles.
    int? resolvedRoleIdFromDto = null;
    string? resolvedJobRoleName = null; // job role stored in AppRoles (e.g., Picker, Packer, Supervisor)
    try
    {
        if (!string.IsNullOrWhiteSpace(dto.StaffRole))
        {
            var trimmed = dto.StaffRole.Trim();
            // Try parse numeric first (some clients might still send an id as string)
            if (int.TryParse(trimmed, out var parsedRoleId))
            {
                resolvedRoleIdFromDto = parsedRoleId;
                // Look up job role name for the role id
                resolvedJobRoleName = await _staffRoleRepo.GetRoleNameByIdAsync(parsedRoleId, ct);
            }
            else
            {
                // Treat StaffRole as job role name (case-insensitive)
                var maybeId = await _staffRoleRepo.GetRoleIdByNameAsync(trimmed, ct);
                if (maybeId.HasValue)
                {
                    resolvedRoleIdFromDto = maybeId.Value;
                    resolvedJobRoleName = await _staffRoleRepo.GetRoleNameByIdAsync(maybeId.Value, ct);
                }
                else
                {
                    // If lookup fails, keep provided value as job role name (best-effort)
                    resolvedJobRoleName = trimmed;
                }
            }
        }
    }
    catch
    {
        // swallow - role resolution is best-effort
    }

        // Create AuthUser entry using email as username if email provided and no existing user
        if (!string.IsNullOrWhiteSpace(staff.Email))
        {
            var existing = await _authRepo.GetByUsernameAsync(staff.Email, ct);
            if (existing == null)
            {
                // Generate a temporary random password (12 chars). Admin can force reset or notify user.
                var tempPassword = Guid.NewGuid().ToString("N").Substring(0, 12);
                var (hash, salt) = _hasher.Hash(tempPassword);
                var authUser = new AuthUser(staff.StaffId, staff.Email, hash, salt, DateTime.UtcNow);

                // Create ApplicationUser with role if specified. Prefer explicit AccessRole provided by client.
                try
                {
                    IEnumerable<string>? roles = null;
                    // If client supplied explicit AccessRole(s), use them (normalizing case)
                        if (dto.AccessRole != null && dto.AccessRole.Length > 0)
                        {
                            // Whitelist allowed identity roles to avoid clients assigning arbitrary roles.
                            var allowed = new[] { "Admin", "User" };
                            var sanitized = dto.AccessRole
                                .Where(r => !string.IsNullOrWhiteSpace(r))
                                .Select(r => r!.Trim())
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .Where(r => allowed.Contains(r, StringComparer.OrdinalIgnoreCase))
                                .ToArray();

                            if (sanitized.Length > 0) roles = sanitized;
                            // If sanitized is empty, fall back to job-role mapping below.
                        }
                    else
                    {
                        // Determine identity role based on resolved job role name (preferred)
                        string? identityRoleToAssign = null;
                        if (!string.IsNullOrWhiteSpace(resolvedJobRoleName))
                        {
                            var jr = resolvedJobRoleName.Trim();
                            identityRoleToAssign = jr.Equals("admin", StringComparison.OrdinalIgnoreCase)
                                ? "Admin"
                                : "User";
                        }
                        // Backward compatibility: if no job role resolved, fall back to explicit StaffRoleId/StaffRole
                        if (identityRoleToAssign == null)
                        {
                            if (dto.StaffRoleId.HasValue)
                            {
                                var role = await _staffRoleRepo.GetRoleNameByIdAsync(dto.StaffRoleId.Value, ct);
                                if (!string.IsNullOrEmpty(role)) identityRoleToAssign = role.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";
                            }
                            else if (!string.IsNullOrWhiteSpace(dto.StaffRole))
                            {
                                identityRoleToAssign = dto.StaffRole!.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(identityRoleToAssign)) roles = new[] { identityRoleToAssign };
                    }

                    var (identityId, error) = await _identityService.CreateUserAsync(staff.Email!, tempPassword, staff.StaffId, roles);
                    if (!string.IsNullOrWhiteSpace(identityId)) 
                    {
                        authUser.LinkIdentityUser(identityId);
                    }
                    else if (!string.IsNullOrWhiteSpace(error))
                    {
                        // Log the error but continue - we still want to create the local auth user
                        // TODO: Add proper logging
                        Console.WriteLine($"Failed to create Identity user: {error}");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue - we still want to create the local auth user
                    // TODO: Add proper logging
                    Console.WriteLine($"Failed to create Identity user: {ex.Message}");
                }

                await _authRepo.AddAsync(authUser, ct);

                // Send new account email with temporary password (best-effort)
                try
                {
                    // best-effort: do not throw if email fails
                    await _emailService.SendNewAccountEmailAsync(staff.Email!, tempPassword);
                }
                catch
                {
                    // intentionally swallow email errors so staff creation still succeeds
                }
            }
        }

        // If a role was resolved from the provided RoleID (preferred), create a StaffRole mapping.
    // Fall back to dto.StaffRoleId / dto.StaffRole for backward-compatibility.
        try
        {
            int? roleToAssign = resolvedRoleIdFromDto;
            if (!roleToAssign.HasValue && dto.StaffRoleId.HasValue)
            {
                roleToAssign = dto.StaffRoleId.Value;
            }
            else if (!roleToAssign.HasValue && !string.IsNullOrWhiteSpace(dto.StaffRole))
            {
                roleToAssign = await _staffRoleRepo.GetRoleIdByNameAsync(dto.StaffRole!, ct);
            }

            if (roleToAssign.HasValue)
            {
                var sr = new FarmManagement.Core.Entities.StaffRole
                {
                    StaffId = staff.StaffId,
                    RoleId = roleToAssign.Value,
                    IsCurrent = true,
                    EffectiveFrom = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                await _staffRoleRepo.AddAsync(sr, ct);
            }
        }
        catch
        {
            // swallow
        }

        // Request a password reset token be sent to the staff email (best-effort)
        try
        {
            if (!string.IsNullOrWhiteSpace(staff.Email))
            {
                await _passwordResetService.SendResetTokenByStaffNumberAsync(staff.StaffNumber, ct);
            }
        }
        catch
        {
            // swallow
        }

        return staff;
    }

    public async Task<bool> UpdateAsync(int staffId, StaffDto dto, CancellationToken ct = default)
    {
        var staff = await _repo.GetByIdAsync(staffId, ct);
        if (staff == null) return false;

        // Do NOT update StaffNumber: it's configured as a key/alternate key in the model
        staff.FirstName = dto.FirstName;
        staff.LastName = dto.LastName;
        staff.Email = dto.Email;
        staff.Phone = dto.Phone;
        staff.Address = dto.Address;
        // Parse ContractType safely (case-insensitive)
        if (!Enum.TryParse<Core.Enums.ContractTypeEnum>(dto.ContractType, true, out var parsedCt))
        {
            throw new ArgumentException($"Invalid contract type '{dto.ContractType}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(Core.Enums.ContractTypeEnum)))}");
        }
        staff.ContractType = parsedCt;
        // Resolve department for update: prefer explicit id, otherwise try name
        if (dto.DepartmentId.HasValue)
        {
            staff.DepartmentId = dto.DepartmentId;
        }
        else if (!string.IsNullOrWhiteSpace(dto.DepartmentName))
        {
            var id = await _departmentRepo.GetDepartmentIdByNameAsync(dto.DepartmentName!, ct);
            if (!id.HasValue)
            {
                throw new ArgumentException($"Department with name '{dto.DepartmentName}' not found.");
            }
            staff.DepartmentId = id.Value;
        }
        staff.HireDate = dto.HireDate;
        staff.TerminationDate = dto.TerminationDate;
    staff.WeeklyAvailableHour = dto.WeeklyAvailableHour;
        staff.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(staff, ct);
        // If role provided on update, try to add a new StaffRole mapping (best-effort)
        try
        {
            int? resolvedRoleId = dto.StaffRoleId;
            if (!resolvedRoleId.HasValue && !string.IsNullOrWhiteSpace(dto.StaffRole))
            {
                resolvedRoleId = await _staffRoleRepo.GetRoleIdByNameAsync(dto.StaffRole!, ct);
            }

            if (resolvedRoleId.HasValue)
            {
                var sr = new FarmManagement.Core.Entities.StaffRole
                {
                    StaffId = staff.StaffId,
                    RoleId = resolvedRoleId.Value,
                    IsCurrent = true,
                    EffectiveFrom = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                await _staffRoleRepo.AddAsync(sr, ct);
            }
        }
        catch
        {
            // swallow
        }
        // If client supplied AccessRole (identity roles) on update, replace the user's identity roles
        try
        {
            if (dto.AccessRole != null && dto.AccessRole.Length > 0)
            {
                // Use the identity service to replace roles (this will ensure roles exist)
                var sanitized = dto.AccessRole.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase);
                await _identityService.SetRolesForStaffAsync(staff.StaffId, sanitized);
            }
        }
        catch
        {
            // swallow - role assignment is best-effort
        }
        return true;
    }

    public async Task<bool> UpdateByStaffNumberAsync(string staffNumber, StaffDto dto, CancellationToken ct = default)
    {
        var existing = await _repo.GetByStaffNumberAsync(staffNumber, ct);
        if (existing == null) return false;
        return await UpdateAsync(existing.StaffId, dto, ct);
    }

    public async Task<bool> DeleteAsync(int staffId, CancellationToken ct = default)
    {
        var staff = await _repo.GetByIdAsync(staffId, ct);
        if (staff == null) return false;

        await _repo.DeleteAsync(staff, ct);
        return true;
    }

    public async Task<bool> DeleteByStaffNumberAsync(string staffNumber, CancellationToken ct = default)
    {
        var existing = await _repo.GetByStaffNumberAsync(staffNumber, ct);
        if (existing == null) return false;
        await _repo.DeleteAsync(existing, ct);
        return true;
    }
}

// using FarmManagement.Core.Entities;
// using FarmManagement.Infrastructure.Data;
// using Microsoft.EntityFrameworkCore;

// namespace FarmManagement.Application.Services;
// public class StaffService
// {
//     private readonly ApplicationDbContext _db; public StaffService(ApplicationDbContext db)=>_db=db;
//     //public async Task<List<Staff>> GetAllAsync(CancellationToken ct=default)=> await _db.Staff.Include(s=>s.Department).AsNoTracking().ToListAsync(ct);
// }
