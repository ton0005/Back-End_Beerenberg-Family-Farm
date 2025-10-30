using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FarmManagement.Core.Entities.Payroll;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class PayrollOptionsSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // if a payroll options row already exists, skip seeding
            if (await db.Set<PayrollOptionEntity>().AnyAsync()) return;

            // Embedded default payroll options (previously in payroll_options_seed.json)
            var entity = new PayrollOptionEntity
            {
                PayFrequency = "Fortnightly",
                FortnightlyDays = 14,
                CasualOvertimeThresholdHours = 12,
                PaidBreakMinutes = 10,
                CreatedAt = DateTime.UtcNow
            };

            db.Set<PayrollOptionEntity>().Add(entity);
            await db.SaveChangesAsync();
        }
        catch
        {
            // best effort - do not block app startup
        }
    }
}
