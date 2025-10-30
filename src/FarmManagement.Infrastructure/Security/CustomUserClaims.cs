using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using FarmManagement.Core.Entities.Identity;
using FarmManagement.Application.Repositories;

namespace FarmManagement.Infrastructure.Security;

public class CustomUserClaims : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly IStaffRoleRepository _staffRoles;

    public CustomUserClaims(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options,
        IStaffRoleRepository staffRoles)
        : base(userManager, roleManager, options)
    {
        _staffRoles = staffRoles;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        // Get the base claims from ASP.NET Core Identity (includes basic user claims)
        var identity = await base.GenerateClaimsAsync(user);

        // Add Staff ID claim
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.StaffId.ToString()));

        // Get staff roles from your StaffRole table
        var staffRoles = await _staffRoles.GetRolesByStaffIdAsync(user.StaffId);
        
        // Add each role as a role claim
        foreach (var role in staffRoles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Role.RoleName));
            // also add a custom 'roles' claim so JwtBearer and code using 'roles' will match
            identity.AddClaim(new Claim("roles", role.Role.RoleName));
        }

        return identity;
    }
}
