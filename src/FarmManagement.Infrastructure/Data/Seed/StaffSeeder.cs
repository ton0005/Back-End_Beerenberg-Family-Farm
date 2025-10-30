using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class StaffSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Seed departments if none exist
            if (!await db.Departments.AnyAsync())
            {
                var deps = new List<FarmManagement.Core.Entities.Department>
                {
                    new() { DepartmentName = "Administration", Description = "Admin and office staff", CreatedAt = DateTime.UtcNow },
                    new() { DepartmentName = "Production", Description = "Field and farm operations", CreatedAt = DateTime.UtcNow },
                    new() { DepartmentName = "Logistics", Description = "Transport and logistics", CreatedAt = DateTime.UtcNow },
                    new() { DepartmentName = "Harvest", Description = "Seasonal and harvest crew", CreatedAt = DateTime.UtcNow }
                };
                db.Departments.AddRange(deps);
                await db.SaveChangesAsync();
            }

            // if there are any staff rows, skip seeding staff
            if (await db.Staff.AnyAsync()) return;

            // pick some existing department ids
            var adminDept = await db.Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Administration");
            var prodDept = await db.Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Production");
            var logDept = await db.Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Logistics");

            // ContractType is seeded via ContractTypeConfiguration (enum), so use known ids: 1=Casual,2=PartTime,3=FullTime
            var staffList = new List<Staff>
            {
                new Staff { StaffNumber = "00002", FirstName = "Alice", LastName = "Johnson", Email = "thanhtruc3995@gmail.com", IsActive = true, DepartmentId = adminDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.FullTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00003", FirstName = "Bob", LastName = "Smith", Email = "bob.smith@farm.local", IsActive = true, DepartmentId = prodDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.PartTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00004", FirstName = "Charlie", LastName = "Brown", Email = "charlie.brown@farm.local", IsActive = false, DepartmentId = logDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.Casual, CreatedAt = DateTime.UtcNow }
            };

            // Add 20 more seeded staff members (mix of departments)
            var harvestDept = await db.Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Harvest");
            // Start numbering from 00010 upwards for new seeds
            var extra = new List<Staff>
            {
                new Staff { StaffNumber = "00010", FirstName = "Diane", LastName = "Miller", Email = "diane.miller@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId ?? prodDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.Casual, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00011", FirstName = "Ethan", LastName = "Wilson", Email = "ethan.wilson@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.PartTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00012", FirstName = "Fiona", LastName = "Taylor", Email = "fiona.taylor@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.FullTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00013", FirstName = "George", LastName = "Anderson", Email = "george.anderson@farm.local", IsActive = true, DepartmentId = prodDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.PartTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00014", FirstName = "Hannah", LastName = "Thomas", Email = "hannah.thomas@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.Casual, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00015", FirstName = "Ian", LastName = "Jackson", Email = "ian.jackson@farm.local", IsActive = false, DepartmentId = logDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.Casual, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00016", FirstName = "Julia", LastName = "White", Email = "julia.white@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.PartTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00017", FirstName = "Kyle", LastName = "Harris", Email = "kyle.harris@farm.local", IsActive = true, DepartmentId = prodDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.FullTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00018", FirstName = "Laura", LastName = "Martin", Email = "laura.martin@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.PartTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00019", FirstName = "Mark", LastName = "Thompson", Email = "mark.thompson@farm.local", IsActive = true, DepartmentId = logDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.FullTime, CreatedAt = DateTime.UtcNow },

                new Staff { StaffNumber = "00020", FirstName = "Nina", LastName = "Garcia", Email = "nina.garcia@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.Casual, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00021", FirstName = "Owen", LastName = "Martinez", Email = "owen.martinez@farm.local", IsActive = true, DepartmentId = prodDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.PartTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00022", FirstName = "Paula", LastName = "Robinson", Email = "paula.robinson@farm.local", IsActive = false, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.Casual, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00023", FirstName = "Quentin", LastName = "Clark", Email = "quentin.clark@farm.local", IsActive = true, DepartmentId = prodDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.FullTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00024", FirstName = "Rita", LastName = "Lewis", Email = "rita.lewis@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.PartTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00025", FirstName = "Sam", LastName = "Lee", Email = "sam.lee@farm.local", IsActive = true, DepartmentId = logDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.Casual, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00026", FirstName = "Tina", LastName = "Walker", Email = "tina.walker@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.FullTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00027", FirstName = "Umar", LastName = "Hall", Email = "umar.hall@farm.local", IsActive = true, DepartmentId = prodDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.PartTime, CreatedAt = DateTime.UtcNow },
                new Staff { StaffNumber = "00028", FirstName = "Vera", LastName = "Allen", Email = "vera.allen@farm.local", IsActive = true, DepartmentId = harvestDept?.DepartmentId, ContractType = FarmManagement.Core.Enums.ContractTypeEnum.Casual, CreatedAt = DateTime.UtcNow }
            };

            staffList.AddRange(extra);

            db.Staff.AddRange(staffList);
            await db.SaveChangesAsync();
        }
        catch
        {
            // best effort seed; swallow exceptions to avoid blocking app start
        }
    }
}
