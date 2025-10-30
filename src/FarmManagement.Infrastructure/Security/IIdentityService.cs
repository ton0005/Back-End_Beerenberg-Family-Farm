using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities.Identity;

namespace FarmManagement.Infrastructure.Security
{
    public class IdentityService : FarmManagement.Application.Services.IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly FarmManagement.Application.Repositories.IStaffRepository _staffRepo;

        public IdentityService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, FarmManagement.Application.Repositories.IStaffRepository staffRepo)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _staffRepo = staffRepo;
        }

        public async Task<(string? userId, string? error)> CreateUserAsync(string email, string password, int staffId, IEnumerable<string>? roles = null)
        {
            try
            {
                // Verify staff exists
                var staff = await _staffRepo.GetByIdAsync(staffId);
                if (staff == null)
                    return (null, "Staff not found");

                var identityUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    StaffId = staffId
                };

                // Ensure required identity fields are set
                if (string.IsNullOrWhiteSpace(identityUser.Id)) identityUser.Id = Guid.NewGuid().ToString();
                if (string.IsNullOrWhiteSpace(identityUser.SecurityStamp)) identityUser.SecurityStamp = Guid.NewGuid().ToString();
                if (string.IsNullOrWhiteSpace(identityUser.ConcurrencyStamp)) identityUser.ConcurrencyStamp = Guid.NewGuid().ToString();

                var createResult = await _userManager.CreateAsync(identityUser, password);
                if (!createResult.Succeeded) 
                    return (null, string.Join(", ", createResult.Errors.Select(e => e.Description)));

                // If roles were provided, add them
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        try
                        {
                            // Ensure the Identity role exists; if not, create it so AddToRoleAsync will succeed
                            if (!await _roleManager.RoleExistsAsync(role))
                            {
                                var createRoleRes = await _roleManager.CreateAsync(new IdentityRole(role));
                                if (!createRoleRes.Succeeded)
                                {
                                    Console.WriteLine($"Failed to create identity role {role}: {string.Join(", ", createRoleRes.Errors.Select(e => e.Description))}");
                                    // continue to next role
                                    continue;
                                }
                            }

                            var roleResult = await _userManager.AddToRoleAsync(identityUser, role);
                            if (!roleResult.Succeeded)
                            {
                                // Log the error but continue - we don't want to fail the whole operation just because role assignment failed
                                Console.WriteLine($"Failed to add role {role}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // swallow but log
                            Console.WriteLine($"Exception while assigning role {role}: {ex.Message}");
                        }
                    }
                }

                return (identityUser.Id, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<string[]?> GetRolesForStaffAsync(int staffId)
        {
            try
            {
                // Find the identity user linked to this staff
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.StaffId == staffId);
                if (user == null) return null;
                var roles = await _userManager.GetRolesAsync(user);
                return roles?.ToArray();
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SetRolesForStaffAsync(int staffId, IEnumerable<string> roles)
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.StaffId == staffId);
                if (user == null) return false;

                var current = await _userManager.GetRolesAsync(user);
                // Remove existing roles
                if (current != null && current.Count > 0)
                {
                    var remRes = await _userManager.RemoveFromRolesAsync(user, current);
                    if (!remRes.Succeeded)
                    {
                        Console.WriteLine($"Failed to remove existing roles: {string.Join(',', remRes.Errors.Select(e=>e.Description))}");
                    }
                }

                foreach (var role in roles)
                {
                    try
                    {
                        if (!await _roleManager.RoleExistsAsync(role))
                        {
                            var createRoleRes = await _roleManager.CreateAsync(new IdentityRole(role));
                            if (!createRoleRes.Succeeded)
                            {
                                Console.WriteLine($"Failed to create identity role {role}: {string.Join(", ", createRoleRes.Errors.Select(e => e.Description))}");
                                continue;
                            }
                        }

                        var addRes = await _userManager.AddToRoleAsync(user, role);
                        if (!addRes.Succeeded)
                        {
                            Console.WriteLine($"Failed to add role {role} to user: {string.Join(", ", addRes.Errors.Select(e => e.Description))}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception while assigning role {role}: {ex.Message}");
                        // continue to next role
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetRolesForStaffAsync failed: {ex.Message}");
                return false;
            }
        }
    }
}
