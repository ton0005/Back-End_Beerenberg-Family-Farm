using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class TimeStationSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();

        try
        {
            if (!await db.TimeStations.AnyAsync())
            {
                var list = new List<TimeStation>
                {
                    new TimeStation { StationName = "Main Gate", Location = "Front", IpAddress = "192.168.1.10", IsActive = true },
                    new TimeStation { StationName = "Barn", Location = "South Barn", IpAddress = "192.168.1.11", IsActive = true }
                };

                db.TimeStations.AddRange(list);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbex)
                {
                    var inner = dbex.InnerException?.Message ?? string.Empty;
                    var msg = inner.ToLowerInvariant();
                    if (msg.Contains("duplicate") || msg.Contains("unique") || msg.Contains("violation of unique") || msg.Contains("cannot insert duplicate key"))
                    {
                        // swallow - another process likely seeded same values
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
        catch
        {
            // best effort
        }
    }
}
