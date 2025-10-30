using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class EntryTypeSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();

        try
        {
            if (!await db.EntryTypes.AnyAsync())
            {
                var list = new List<EntryType>
                {
                    new EntryType { TypeName = "CLOCK_IN" },
                    new EntryType { TypeName = "CLOCK_OUT" },
                    new EntryType { TypeName = "BREAK_START" },
                    new EntryType { TypeName = "BREAK_END" }
                };

                db.EntryTypes.AddRange(list);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbex)
                {
                    // Best-effort: swallow unique constraint / duplicate key errors that can occur
                    // in concurrent seed scenarios. We look at the inner exception message for
                    // common text used by SQL Server or other providers. If it's not clearly a
                    // duplicate key violation, rethrow to surface unexpected DB issues.
                    var inner = dbex.InnerException?.Message ?? string.Empty;
                    var msg = inner.ToLowerInvariant();
                    if (msg.Contains("duplicate") || msg.Contains("unique") || msg.Contains("violation of unique") || msg.Contains("cannot insert duplicate key"))
                    {
                        // swallow - likely another process seeded same values concurrently
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
            // best effort: swallow any other errors to avoid blocking app startup
        }
    }
}
