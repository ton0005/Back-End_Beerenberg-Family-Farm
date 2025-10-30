using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities.Payroll;
using FarmManagement.Core.Enums;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class PayRateSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // if there are any pay rates already, skip seeding
            if (await db.PayRates.AnyAsync()) return;

            var rates = new List<PayRate>
            {
                new PayRate
                {
                    ContractType = ContractTypeEnum.FullTime,
                    RateType = "Regular",
                    HourlyRate = 24.28m,
                    EffectiveFrom = new DateTime(2025,10,19),
                    IsActive = true,
                    Description = "Full-time base rate (Horticulture Award)",
                    CreatedAt = DateTime.UtcNow
                },
                new PayRate
                {
                    ContractType = ContractTypeEnum.PartTime,
                    RateType = "Regular",
                    HourlyRate = 24.28m,
                    EffectiveFrom = new DateTime(2025,10,19),
                    IsActive = true,
                    Description = "Part-time base rate (Horticulture Award)",
                    CreatedAt = DateTime.UtcNow
                },
                new PayRate
                {
                    ContractType = ContractTypeEnum.Casual,
                    RateType = "Regular",
                    HourlyRate = 30.35m,
                    EffectiveFrom = new DateTime(2025,10,19),
                    IsActive = true,
                    Description = "Casual base rate (includes casual loading)",
                    CreatedAt = DateTime.UtcNow
                },
                new PayRate
                {
                    ContractType = ContractTypeEnum.Casual,
                    RateType = "Overtime",
                    HourlyRate = 42.49m,
                    EffectiveFrom = new DateTime(2025,10,19),
                    IsActive = true,
                    Description = "Casual overtime rate (>12 hours/day)",
                    CreatedAt = DateTime.UtcNow
                }
            };

            db.PayRates.AddRange(rates);
            await db.SaveChangesAsync();
        }
        catch
        {
            // best effort - do not block app startup
        }
    }
}
