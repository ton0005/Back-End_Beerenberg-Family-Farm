using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FarmManagement.Application.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using FarmManagement.Core.Entities.Identity;

namespace FarmManagement.Infrastructure.Security
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser>? _userManager;
        private readonly FarmManagement.Application.Repositories.IStaffRoleRepository? _staffRolesRepo;

        public JwtTokenService(IConfiguration config, UserManager<ApplicationUser>? userManager = null, FarmManagement.Application.Repositories.IStaffRoleRepository? staffRolesRepo = null)
        {
            _config = config;
            _userManager = userManager;
            _staffRolesRepo = staffRolesRepo;
        }

        // staffId maps to user Id in Identity; if a UserManager is supplied, attempt to include roles
        public string CreateToken(string staffId, string email, int expiresMinutes = 60)
        {
            var section = _config.GetSection("Jwt");
            var issuer = section["Issuer"]!;
            var audience = section["Audience"]!;
            var key = section["Key"]!;
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256
            );

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, staffId),
                new(JwtRegisteredClaimNames.Email, email),
                new("uid", staffId),
                // Include standard NameIdentifier to maximize compatibility
                new(ClaimTypes.NameIdentifier, staffId)
            };

            if (_userManager != null)
            {
                // try find by provided id first, then fallback to email if not found
                var user = _userManager.FindByIdAsync(staffId).GetAwaiter().GetResult();
                if (user == null && !string.IsNullOrWhiteSpace(email))
                {
                    user = _userManager.FindByEmailAsync(email).GetAwaiter().GetResult();
                }

                if (user != null)
                {
                    // Add numeric staffId claim if available and > 0
                    if (user.StaffId > 0 && !claims.Any(c => c.Type == "staffId"))
                    {
                        claims.Add(new Claim("staffId", user.StaffId.ToString()));
                    }

                    // Add Identity roles (if any)
                    var idRoles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
                    foreach (var r in idRoles)
                    {
                        // Add both custom 'roles' and standard Role claims
                        if (!claims.Any(c => c.Type == "roles" && c.Value == r))
                            claims.Add(new Claim("roles", r));
                        if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == r))
                            claims.Add(new Claim(ClaimTypes.Role, r));
                    }

                    // Also include application staff roles from StaffRole table (if repo available)
                    try
                    {
                        if (_staffRolesRepo != null && user.StaffId != 0)
                        {
                            var staffRoles = _staffRolesRepo.GetRolesByStaffIdAsync(user.StaffId).GetAwaiter().GetResult();
                            foreach (var sr in staffRoles)
                            {
                                if (!string.IsNullOrWhiteSpace(sr.Role?.RoleName))
                                {
                                    if (!claims.Any(c => c.Type == "roles" && c.Value == sr.Role.RoleName))
                                        claims.Add(new Claim("roles", sr.Role.RoleName));
                                    if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == sr.Role.RoleName))
                                        claims.Add(new Claim(ClaimTypes.Role, sr.Role.RoleName));
                                }
                            }
                        }
                    }
                    catch { /* best effort */ }
                }
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}