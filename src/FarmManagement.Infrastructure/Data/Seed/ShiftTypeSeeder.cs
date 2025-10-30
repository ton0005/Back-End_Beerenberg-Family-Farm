using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class ShiftTypeSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();

        try
        {
            if (!await db.ShiftTypes.AnyAsync())
            {
                var list = new List<ShiftType>
                {
                    new ShiftType { Name = "Morning", DefaultStartTime = new TimeSpan(7,0,0), DefaultEndTime = new TimeSpan(12,0,0), Description = "Morning shift (07:00-12:00)" },
                    new ShiftType { Name = "Afternoon", DefaultStartTime = new TimeSpan(13,0,0), DefaultEndTime = new TimeSpan(18,0,0), Description = "Afternoon shift (13:00-18:00)" },
                    new ShiftType { Name = "FullDay", DefaultStartTime = new TimeSpan(9,0,0), DefaultEndTime = new TimeSpan(17,0,0), Description = "Full day shift (09:00-17:00)" },
                    new ShiftType { Name = "Custom", DefaultStartTime = TimeSpan.Zero, DefaultEndTime = TimeSpan.Zero, Description = "Custom shift - specify start and end times" }
                };

                db.ShiftTypes.AddRange(list);
                await db.SaveChangesAsync();
            }
        }
        catch
        {
            // best effort
        }
    }
}
