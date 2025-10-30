using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Seed roles if none exist
            if (!await db.AppRoles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role { RoleName = "Admin", Description = "Full administrator with system privileges" },
                    new Role { RoleName = "Supervisor", Description = "Supervisor role" },
                    new Role { RoleName = "Picker", Description = "Picker role" },
                    new Role { RoleName = "Packer", Description = "Packer role" },
                    new Role { RoleName = "Checker", Description = "Checker role" },
                    new Role { RoleName = "Forklift Driver", Description = "Forklift driver role" }
                };

                db.AppRoles.AddRange(roles);
                await db.SaveChangesAsync();
            }

            // Seed staff-role mappings if none exist
            if (!await db.StaffRoles.AnyAsync())
            {
                // Find admin staff by configured staff number, fallback to email-based staff used by IdentitySeeder
                // Default admin staff number uses 5-digit numeric format
                var adminStaffNumber = config["IdentitySeed:AdminStaffNumber"] ?? "00001";
                var adminStaff = await db.Staff.FirstOrDefaultAsync(s => s.StaffNumber.Trim() == adminStaffNumber) 
                                 ?? await db.Staff.FirstOrDefaultAsync(s => s.Email == (config["IdentitySeed:AdminEmail"] ?? "admin@farm.local"));

                // use case-insensitive lookup for role names
                var adminRole = await db.AppRoles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == "admin");

                var seedList = new List<StaffRole>();

                if (adminStaff != null && adminRole != null)
                {
                    seedList.Add(new StaffRole
                    {
                        StaffId = adminStaff.StaffId,
                        RoleId = adminRole.RoleId,
                        IsCurrent = true,
                        EffectiveFrom = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // assign Supervisor to staff with number 00002 if present
                var staffStf1 = await db.Staff.FirstOrDefaultAsync(s => (s.StaffNumber ?? string.Empty).Trim() == "00002");
                var supervisorRole = await db.AppRoles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == "supervisor");
                if (staffStf1 != null && supervisorRole != null)
                {
                    seedList.Add(new StaffRole
                    {
                        StaffId = staffStf1.StaffId,
                        RoleId = supervisorRole.RoleId,
                        IsCurrent = true,
                        EffectiveFrom = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (seedList.Any())
                {
                    db.StaffRoles.AddRange(seedList);
                    await db.SaveChangesAsync();
                }
            }
        }
        catch
        {
            // best-effort seeding
        }
    }
}
