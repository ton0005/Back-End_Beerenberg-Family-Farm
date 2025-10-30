using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FarmManagement.Core.Entities;
using FarmManagement.Core.Enums;

namespace FarmManagement.Infrastructure.Data.Seed;

public static class TestPayrollDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Configurable options
            var staffNumbersCsv = config["Seed:TestPayroll:StaffNumbers"]; // comma-separated; optional
            var staffNumberSingle = config["Seed:TestPayroll:StaffNumber"]; // legacy/single
            var staffCountStr = config["Seed:TestPayroll:StaffCount"]; // number of auto-generated staff
            var startDateStr = config["Seed:TestPayroll:StartDate"]; // inclusive
            var endDateStr = config["Seed:TestPayroll:EndDate"];     // inclusive
            var daysStr = config["Seed:TestPayroll:Days"];           // alternative to EndDate
            var includeWeekend = bool.TryParse(config["Seed:TestPayroll:IncludeWeekend"], out var iw) ? iw : true;
            var variationMinutes = int.TryParse(config["Seed:TestPayroll:VariationMinutes"], out var vm) ? Math.Max(0, vm) : 15;
            var twoSessionsEveryN = int.TryParse(config["Seed:TestPayroll:TwoSessionsEveryNDays"], out var tsn) ? Math.Max(0, tsn) : 0;
            var overtimeEveryN = int.TryParse(config["Seed:TestPayroll:OvertimeEveryNDays"], out var otn) ? Math.Max(0, otn) : 0;
            var shiftTypesCsv = config["Seed:TestPayroll:ShiftTypes"]; // e.g. "FullDay,Morning,Afternoon"

            // Build staff list
            var staffNumbers = new List<string>();
            if (!string.IsNullOrWhiteSpace(staffNumbersCsv))
            {
                staffNumbers = staffNumbersCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(staffNumberSingle))
            {
                staffNumbers.Add(staffNumberSingle.Trim());
            }
            else
            {
                var count = int.TryParse(staffCountStr, out var sc) ? Math.Max(1, sc) : 1;
                for (int i = 1; i <= count; i++) staffNumbers.Add($"TEST{i:000}");
            }

            // Build date range
            DateTime startDate;
            DateTime endDate;
            if (!DateTime.TryParse(startDateStr, out startDate))
            {
                startDate = DateTime.UtcNow.Date.AddDays(-6); // default last 7 days
            }
            if (DateTime.TryParse(endDateStr, out endDate))
            {
                // keep
            }
            else if (int.TryParse(daysStr, out var days) && days > 0)
            {
                endDate = startDate.AddDays(days - 1);
            }
            else
            {
                endDate = DateTime.UtcNow.Date; // inclusive
            }

            // Ensure EntryTypes exist (CLOCK_IN/OUT, BREAK_START/END) - seeded elsewhere
            var clockIn = await db.EntryTypes.FirstOrDefaultAsync(e => e.TypeName == "CLOCK_IN");
            var clockOut = await db.EntryTypes.FirstOrDefaultAsync(e => e.TypeName == "CLOCK_OUT");
            var breakStart = await db.EntryTypes.FirstOrDefaultAsync(e => e.TypeName == "BREAK_START");
            var breakEnd = await db.EntryTypes.FirstOrDefaultAsync(e => e.TypeName == "BREAK_END");
            if (clockIn == null || clockOut == null || breakStart == null || breakEnd == null)
            {
                // EntryTypeSeeder must have run; abort quietly
                return;
            }

            // Ensure a time station exists
            var station = await db.TimeStations.FirstOrDefaultAsync();
            if (station == null)
            {
                station = new TimeStation { StationName = "Test Station", Location = "Test", IpAddress = "127.0.0.1", IsActive = true };
                db.TimeStations.Add(station);
                await db.SaveChangesAsync();
            }
            // Resolve shift types list (prefer ordered by provided names, else fallback to FullDay/any)
            List<ShiftType> shiftTypes = new();
            if (!string.IsNullOrWhiteSpace(shiftTypesCsv))
            {
                var names = shiftTypesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var name in names)
                {
                    var t = await db.ShiftTypes.FirstOrDefaultAsync(x => x.Name == name);
                    if (t != null) shiftTypes.Add(t);
                }
            }
            if (shiftTypes.Count == 0)
            {
                var fullDayType = await db.ShiftTypes.FirstOrDefaultAsync(t => t.Name == "FullDay");
                if (fullDayType != null) shiftTypes.Add(fullDayType);
                var morning = await db.ShiftTypes.FirstOrDefaultAsync(t => t.Name == "Morning");
                var afternoon = await db.ShiftTypes.FirstOrDefaultAsync(t => t.Name == "Afternoon");
                if (morning != null) shiftTypes.Add(morning);
                if (afternoon != null) shiftTypes.Add(afternoon);
            }
            if (shiftTypes.Count == 0)
            {
                // create minimal FullDay if none exist
                var anyType = new ShiftType { Name = "FullDay", DefaultStartTime = new TimeSpan(9,0,0), DefaultEndTime = new TimeSpan(17,0,0), Description = "Full day" };
                db.ShiftTypes.Add(anyType);
                await db.SaveChangesAsync();
                shiftTypes.Add(anyType);
            }

            // Ensure all staff exist
            var staffMap = new Dictionary<string, Staff>(StringComparer.OrdinalIgnoreCase);
            foreach (var sn in staffNumbers)
            {
                var st = await db.Staff.FirstOrDefaultAsync(s => s.StaffNumber == sn);
                if (st == null)
                {
                    st = new Staff
                    {
                        StaffNumber = sn,
                        FirstName = "Test",
                        LastName = "User",
                        Email = $"{sn.ToLower()}@farm.local",
                        ContractType = ContractTypeEnum.Casual,
                        WeeklyAvailableHour = 40,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Staff.Add(st);
                    await db.SaveChangesAsync();
                }
                staffMap[sn] = st;
            }

            // Iterate dates and generate sessions
            var allDates = Enumerable.Range(0, (endDate.Date - startDate.Date).Days + 1)
                                     .Select(i => startDate.Date.AddDays(i))
                                     .Where(d => includeWeekend || (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday))
                                     .ToList();

            for (int dIdx = 0; dIdx < allDates.Count; dIdx++)
            {
                var workDate = allDates[dIdx];
                // pick shift type round-robin
                var chosenType = shiftTypes[dIdx % shiftTypes.Count];

                // Ensure a shift for this date+type
                var shift = await db.Shifts.FirstOrDefaultAsync(s => s.Date.Date == workDate.Date && s.ShiftTypeId == chosenType.ShiftTypeId);
                if (shift == null)
                {
                    var defaultStart = chosenType.DefaultStartTime == TimeSpan.Zero ? new TimeSpan(8,0,0) : chosenType.DefaultStartTime;
                    var defaultEnd = chosenType.DefaultEndTime == TimeSpan.Zero ? new TimeSpan(16,30,0) : chosenType.DefaultEndTime;
                    shift = new Shift
                    {
                        ShiftTypeId = chosenType.ShiftTypeId,
                        Date = workDate.Date,
                        StartTime = defaultStart,
                        EndTime = defaultEnd,
                        Note = "Test Payroll Shift",
                        IsPublished = true
                    };
                    db.Shifts.Add(shift);
                    await db.SaveChangesAsync();
                }

                foreach (var sn in staffNumbers)
                {
                    var staff = staffMap[sn];
                    // Ensure assignment exists
                    var assignment = await db.ShiftAssignments.FirstOrDefaultAsync(a => a.ShiftId == shift.ShiftId && a.StaffId == staff.StaffId);
                    if (assignment == null)
                    {
                        assignment = new ShiftAssignment
                        {
                            ShiftId = shift.ShiftId,
                            StaffId = staff.StaffId,
                            Status = FarmManagement.Core.Enums.AssignmentStatusEnum.Assigned,
                            AssignedAt = DateTime.UtcNow
                        };
                        db.ShiftAssignments.Add(assignment);
                        await db.SaveChangesAsync();
                    }

                    // Skip if entries already exist for staff/date
                    bool hasEntries = await db.TimeEntries.AnyAsync(e => e.StaffNumber == sn && e.EntryTimestamp.Date == workDate.Date);
                    if (hasEntries) continue;

                    // Determine sessions for the day
                    var useTwoSessions = twoSessionsEveryN > 0 && ((dIdx + 1) % twoSessionsEveryN == 0);
                    var isOvertimeDay = overtimeEveryN > 0 && ((dIdx + 1) % overtimeEveryN == 0);

                    // Create deterministic but varied offsets
                    int Seed(string s) => unchecked(s.GetHashCode());
                    var rand = new Random(Seed($"{sn}|{workDate:yyyyMMdd}"));
                    int Offset() => variationMinutes == 0 ? 0 : rand.Next(-variationMinutes, variationMinutes + 1);

                    var entries = new List<TimeEntry>();

                    if (!useTwoSessions)
                    {
                        // Single session
                        var start = workDate.Add((shift.StartTime ?? new TimeSpan(8,0,0))).AddMinutes(Offset());
                        var end = workDate.Add((shift.EndTime ?? new TimeSpan(16,30,0))).AddMinutes(Offset());
                        if (isOvertimeDay) end = end.AddHours(1);
                        if (end <= start) end = start.AddHours(8); // safety

                        var breakStartTime = start.AddMinutes(((end - start).TotalMinutes / 2)).AddMinutes(-15);
                        var breakEndTime = breakStartTime.AddMinutes(30);

                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = clockIn.EntryTypeId, EntryTimestamp = start, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });
                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = breakStart.EntryTypeId, EntryTimestamp = breakStartTime, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });
                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = breakEnd.EntryTypeId, EntryTimestamp = breakEndTime, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });
                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = clockOut.EntryTypeId, EntryTimestamp = end, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });

                        // Mark assignment completed at last clock out
                        assignment.CompletedAt = end;
                    }
                    else
                    {
                        // Two sessions (morning + afternoon)
                        var morningStart = workDate.AddHours(8).AddMinutes(Offset());
                        var morningEnd = workDate.AddHours(12).AddMinutes(Offset());
                        var afternoonStart = workDate.AddHours(13).AddMinutes(Offset());
                        var afternoonEnd = workDate.AddHours(16).AddMinutes(30 + Offset());
                        if (isOvertimeDay) afternoonEnd = afternoonEnd.AddHours(1);
                        if (morningEnd <= morningStart) morningEnd = morningStart.AddHours(3.5);
                        if (afternoonEnd <= afternoonStart) afternoonEnd = afternoonStart.AddHours(3.5);

                        // Short breaks mid-session
                        var mBreakStart = morningStart.AddMinutes(((morningEnd - morningStart).TotalMinutes / 2) - 5);
                        var mBreakEnd = mBreakStart.AddMinutes(10);
                        var aBreakStart = afternoonStart.AddMinutes(((afternoonEnd - afternoonStart).TotalMinutes / 2) - 5);
                        var aBreakEnd = aBreakStart.AddMinutes(10);

                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = clockIn.EntryTypeId, EntryTimestamp = morningStart, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });
                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = breakStart.EntryTypeId, EntryTimestamp = mBreakStart, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });
                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = breakEnd.EntryTypeId, EntryTimestamp = mBreakEnd, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });
                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = clockOut.EntryTypeId, EntryTimestamp = morningEnd, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });

                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = clockIn.EntryTypeId, EntryTimestamp = afternoonStart, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });
                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = breakStart.EntryTypeId, EntryTimestamp = aBreakStart, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });
                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = breakEnd.EntryTypeId, EntryTimestamp = aBreakEnd, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });
                        entries.Add(new TimeEntry { StaffNumber = sn, StationId = station.StationId, EntryTypeId = clockOut.EntryTypeId, EntryTimestamp = afternoonEnd, ShiftAssignmentId = assignment.ShiftAssignmentId, CreatedAt = DateTime.UtcNow });

                        assignment.CompletedAt = afternoonEnd;
                    }

                    db.TimeEntries.AddRange(entries);
                    await db.SaveChangesAsync();
                    await db.SaveChangesAsync();
                }
            }
        }
        catch
        {
            // best effort - do not block startup
        }
    }
}
