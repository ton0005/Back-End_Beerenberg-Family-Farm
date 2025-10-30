using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class ExceptionTypeSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<ApplicationDbContext>();

        try
        {
            if (!await db.ExceptionTypes.AnyAsync())
            {
                var list = new List<ExceptionType>
                {
                    new ExceptionType { TypeName = "MISSING_CLOCK_IN", Description = "Staff forgot to clock in" },
                    new ExceptionType { TypeName = "MISSING_CLOCK_OUT", Description = "Staff forgot to clock out" },
                    new ExceptionType { TypeName = "ADJUST_REQUEST", Description = "Staff requests adjustment to an existing entry" },
                    new ExceptionType { TypeName = "INCORRECT_STATION", Description = "Entry recorded at incorrect station" },
                    new ExceptionType { TypeName = "OTHER", Description = "Other / miscellaneous exception" }
                };

                db.ExceptionTypes.AddRange(list);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException dbex)
                {
                    var inner = dbex.InnerException?.Message ?? string.Empty;
                    var msg = inner.ToLowerInvariant();
                    if (msg.Contains("duplicate") || msg.Contains("unique") || msg.Contains("violation of unique") || msg.Contains("cannot insert duplicate key"))
                    {
                        // swallow expected duplicate from concurrency
                    }
                    else throw;
                }
            }
        }
        catch
        {
            // best effort: do not block startup
        }
    }
}