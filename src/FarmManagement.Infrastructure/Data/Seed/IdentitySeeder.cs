using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Infrastructure.Data;
using FarmManagement.Core.Entities;
using FarmManagement.Core.Entities.Identity;
using FarmManagement.Application.Security;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

    var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher>();

        var adminEmail = config["IdentitySeed:AdminEmail"] ?? "ton0005@flinders.edu.au";
        var adminPassword = config["IdentitySeed:AdminPassword"] ?? "Admin@12345";
        var roles = new[] { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Ensure Administration department and the admin staff record exist before creating the Identity user
        Department? adminDept = null;
        Staff? staff = null;
        try
        {
            adminDept = await db.Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Administration");
            if (adminDept == null)
            {
                adminDept = new Department { DepartmentName = "Administration", Description = "Admin and office staff", CreatedAt = DateTime.UtcNow };
                db.Departments.Add(adminDept);
                await db.SaveChangesAsync();
            }

            staff = await db.Staff.FirstOrDefaultAsync(s => s.Email == adminEmail || (s.StaffNumber != null && s.StaffNumber.Trim() == (config["IdentitySeed:AdminStaffNumber"] ?? "00001")));
            if (staff == null)
            {
                var staffNumber = config["IdentitySeed:AdminStaffNumber"] ?? "00001";
                staff = new Staff
                {
                    StaffNumber = staffNumber,
                    FirstName = config["IdentitySeed:AdminFirstName"] ?? "Admin",
                    LastName = config["IdentitySeed:AdminLastName"] ?? "User",
                    Email = adminEmail,
                    IsActive = true,
                    DepartmentId = adminDept.DepartmentId,
                    ContractType = FarmManagement.Core.Enums.ContractTypeEnum.FullTime,
                    CreatedAt = DateTime.UtcNow
                };
                db.Staff.Add(staff);
                await db.SaveChangesAsync();
            }
        }
        catch
        {
            // best effort; continue to attempt Identity creation even if staff seeding has issues
        }

        // Create or update the Identity user and ensure it references the StaffId
        var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
        {
            var newAdmin = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, StaffId = staff?.StaffId ?? 0 };
            var result = await userManager.CreateAsync(newAdmin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdmin, "Admin");
                // Reload admin from DB to ensure Id and StaffId are set
                admin = await userManager.FindByEmailAsync(adminEmail);
            }
            else
            {
                // creation failed - attempt to load existing user if any
                admin = await userManager.FindByEmailAsync(adminEmail);
            }
        }
        else
        {
            // ensure admin has the role
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            // ensure StaffId on Identity user is set
            if (staff != null && admin is ApplicationUser appUser && appUser.StaffId != staff.StaffId)
            {
                appUser.StaffId = staff.StaffId;
                await userManager.UpdateAsync(appUser);
                // reload
                admin = await userManager.FindByEmailAsync(adminEmail);
            }
        }

            // Link Identity user to Staff record by setting StaffId on the ApplicationUser
            if (admin != null && staff != null)
            {
                // If StaffId not set or mismatched, update the ApplicationUser
                if (admin.StaffId != staff.StaffId)
                {
                    admin.StaffId = staff.StaffId;
                    // Update via UserManager to ensure proper tracking
                    await userManager.UpdateAsync(admin);
                    // reload admin to be safe
                    admin = await userManager.FindByEmailAsync(adminEmail);
                }
            }

        // Ensure admin also exists as Staff and has an AuthUser record linking to Staff
        try
        {
            // Ensure Administration department exists (admin is in HR/Administration)
            adminDept = adminDept ?? await db.Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Administration");
            if (adminDept == null)
            {
                adminDept = new Department { DepartmentName = "Administration", Description = "Admin and office staff", CreatedAt = DateTime.UtcNow };
                db.Departments.Add(adminDept);
                await db.SaveChangesAsync();
            }

            // Find or create staff by email (reuse staff variable)
            staff = staff ?? await db.Staff.FirstOrDefaultAsync(s => s.Email == adminEmail || (s.StaffNumber != null && s.StaffNumber.Trim() == (config["IdentitySeed:AdminStaffNumber"] ?? "00001")));
            if (staff == null)
            {
                // Use 5-digit numeric staff numbers to match API validation (e.g. "00001")
                var staffNumber = config["IdentitySeed:AdminStaffNumber"] ?? "00001";
                staff = new Staff
                {
                    StaffNumber = staffNumber,
                    FirstName = config["IdentitySeed:AdminFirstName"] ?? "Admin",
                    LastName = config["IdentitySeed:AdminLastName"] ?? "User",
                    Email = adminEmail,
                    IsActive = true,
                    DepartmentId = adminDept.DepartmentId,
                    ContractType = FarmManagement.Core.Enums.ContractTypeEnum.FullTime,
                    CreatedAt = DateTime.UtcNow
                };
                db.Staff.Add(staff);
                await db.SaveChangesAsync();
            }

            // Ensure an AuthUser record exists for this staff (site-local auth store)
            var existingAuth = await db.AuthUsers.FirstOrDefaultAsync(a => a.Username == adminEmail);
            var identityUserId = admin?.Id;
            if (existingAuth == null)
            {
                // Hash the same password used for Identity local store so both match expectations for API auth
                var (hash, salt) = hasher.Hash(adminPassword);
                var authUser = new AuthUser(staff.StaffId, adminEmail, hash, salt, DateTime.UtcNow);
                // link to the AspNet Identity user if available
                if (!string.IsNullOrEmpty(identityUserId))
                {
                    authUser.LinkIdentityUser(identityUserId);
                }
                db.AuthUsers.Add(authUser);
                await db.SaveChangesAsync();
            }
            else
            {
                // ensure existing record is linked if not already
                if (string.IsNullOrEmpty(existingAuth.IdentityUserId) && !string.IsNullOrEmpty(identityUserId))
                {
                    existingAuth.LinkIdentityUser(identityUserId);
                    db.AuthUsers.Update(existingAuth);
                    await db.SaveChangesAsync();
                }
            }
        }
        catch
        {
            // best effort; do not block app startup
        }
    }
}
