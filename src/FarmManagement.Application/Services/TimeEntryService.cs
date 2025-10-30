using FarmManagement.Application.DTOs;
using FarmManagement.Application.Repositories;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace FarmManagement.Application.Services
{
    public class TimeEntryService : ITimeEntryService
    {
        // per-staff in-memory locks to serialize clock operations within this process
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Threading.SemaphoreSlim> _staffLocks = new();
    private readonly ITimeEntryRepository _repo;
    private readonly IExceptionRepository _excRepo;
    private readonly IAuditRepository _auditRepo;
        private readonly IShiftService _shiftService;
        private readonly IEntryTypeRepository _entryTypeRepo;
        private readonly IShiftAssignmentRepository _assignmentRepo;
        private readonly bool _allowMultipleSessionsPerDay;
    private readonly ILogger<TimeEntryService> _logger;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public TimeEntryService(ITimeEntryRepository repo, IExceptionRepository excRepo, IAuditRepository auditRepo, IShiftService shiftService, IEntryTypeRepository entryTypeRepo, IShiftAssignmentRepository assignmentRepo, Microsoft.Extensions.Configuration.IConfiguration config, ILogger<TimeEntryService> logger, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _repo = repo; _excRepo = excRepo; _auditRepo = auditRepo; _shiftService = shiftService; _entryTypeRepo = entryTypeRepo; _assignmentRepo = assignmentRepo;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            // Read flag (default false)
            _allowMultipleSessionsPerDay = config.GetValue<bool>("TimeTracking:AllowMultipleSessionsPerDay");
        }


        public async Task<TimeEntryDto> ClockAsync(TimeEntryDto dto, CancellationToken ct = default)
        {
            var entryTimestamp = dto.EntryTimestamp == default ? DateTime.UtcNow : dto.EntryTimestamp;

            _logger?.LogInformation("ClockAsync START: StaffNumber={StaffNumber}, EntryTypeId={EntryTypeId}, ProvidedTimestamp={ProvidedTimestamp}, CalculatedTimestamp={CalculatedTimestamp}", 
                dto.StaffNumber, dto.EntryTypeId, dto.EntryTimestamp, entryTimestamp);

            // Ensure staff number is provided and normalized. Controller usually enforces this, but service should validate too.
            if (string.IsNullOrWhiteSpace(dto.StaffNumber))
                throw new ArgumentException("StaffNumber is required");

            var staffNumber = dto.StaffNumber!.Trim();

            // Resolve current entry type (required for sequence validation)
            var currentEntryType = await _entryTypeRepo.GetByIdAsync(dto.EntryTypeId);
            if (currentEntryType == null)
                throw new ArgumentException("Invalid EntryTypeId");
            var currentTypeName = currentEntryType.TypeName?.Trim().ToUpperInvariant() ?? string.Empty;

            // By default we validate shift assignments. If caller set BypassShiftValidation (controller restricts to admins)
            // then skip validation entirely and persist no ShiftAssignmentId unless provided explicitly.
            int? resolvedAssignmentId = null;
            if (dto.BypassShiftValidation != true)
            {
                // Load assignments for the staff
                var assignments = (await _shiftService.GetAssignmentsByStaffNumberAsync(staffNumber)).ToList();

                if (dto.ShiftAssignmentId.HasValue)
                {
                    var match = assignments.FirstOrDefault(a => a.ShiftAssignmentId == dto.ShiftAssignmentId.Value);
                    if (match == null)
                        throw new ArgumentException("Shift assignment not found for staff");

                    var shift = await _shiftService.GetShiftByIdAsync(match.ShiftId);
                    if (shift == null || shift.Date != entryTimestamp.Date)
                        throw new ArgumentException("Shift assignment is not for the entry date");

                    resolvedAssignmentId = dto.ShiftAssignmentId.Value;
                }
                else
                {
                    // Try to find any assignment for the same date
                    ShiftAssignmentDto? found = null;
                    foreach (var a in assignments)
                    {
                        var shift = await _shiftService.GetShiftByIdAsync(a.ShiftId);
                        if (shift != null && shift.Date == entryTimestamp.Date)
                        {
                            found = a;
                            break;
                        }
                    }

                    if (found == null)
                        throw new ArgumentException("No shift assignment found for staff on the entry date");

                    resolvedAssignmentId = found.ShiftAssignmentId;
                }
            }

            // Sequence / state validation (per staff per day)
            // Business rules:
            // 1. Must CLOCK_IN before any other event.
            // 2. Cannot CLOCK_IN again if an open work session exists (CLOCK_IN without CLOCK_OUT).
            // 3. BREAK_START allowed only when clocked in, not currently in an open break, and before CLOCK_OUT.
            // 4. BREAK_END allowed only when a BREAK_START is open.
            // 5. CLOCK_OUT allowed only when clocked in and no open break is active.
            // 6. No break events permitted after CLOCK_OUT or before first CLOCK_IN.
            // If _allowMultipleSessionsPerDay is true, a second CLOCK_IN is permitted after a completed prior session (CLOCK_IN -> CLOCK_OUT) sequence.
            var entryDate = DateOnly.FromDateTime(entryTimestamp);
            var todaysEntries = (await _repo.GetByStaffNumberAndDateAsync(staffNumber, entryDate, ct))
                .OrderBy(e => e.EntryTimestamp)
                .ToList();

            // track number of unmatched CLOCK_IN entries (allows robust handling of mismatched/extra records)
            int openClockCount = 0;       // >0 means there is at least one open clock-in session
            bool inBreak = false;         // true if last unmatched BREAK_START present

            // Prefetch entry type names asynchronously for all types referenced in today's entries plus the requested type
            var typeNameCache = new Dictionary<int, string>();
            var neededTypeIds = todaysEntries.Select(e => e.EntryTypeId).Append(dto.EntryTypeId).Distinct().ToList();
            foreach (var id in neededTypeIds)
            {
                try
                {
                    var et = await _entryTypeRepo.GetByIdAsync(id);
                    var name = (et?.TypeName ?? string.Empty).Trim().ToUpperInvariant();
                    typeNameCache[id] = name;
                }
                catch
                {
                    typeNameCache[id] = string.Empty;
                }
            }

            string ResolveTypeName(int id)
            {
                return typeNameCache.TryGetValue(id, out var n) ? n : string.Empty;
            }

            foreach (var e in todaysEntries)
            {
                var n = ResolveTypeName(e.EntryTypeId);
                switch (n)
                {
                    case "CLOCK_IN":
                        openClockCount++;
                        break;
                    case "BREAK_START":
                        // Only mark break if there is an open work session
                        if (openClockCount > 0)
                            inBreak = true;
                        break;
                    case "BREAK_END":
                        if (inBreak) inBreak = false; // else ignore inconsistent
                        break;
                    case "CLOCK_OUT":
                        // Only consume an open clock if one exists; otherwise ignore extra CLOCK_OUT records
                        if (openClockCount > 0) openClockCount--;
                        inBreak = false; // any open break logically closed
                        break;
                }
            }

            // Acquire a per-staff lock to avoid concurrent race conditions where two requests both validate and insert
            var sem = _staffLocks.GetOrAdd(staffNumber, _ => new System.Threading.SemaphoreSlim(1, 1));
            await sem.WaitAsync(ct);
            try
            {
                // Recalculate current state inside the lock in case other thread modified entries in the meantime
                var refreshed = (await _repo.GetByStaffNumberAndDateAsync(staffNumber, entryDate, ct)).OrderBy(e => e.EntryTimestamp).ToList();
                
                _logger?.LogInformation("ClockAsync: Found {Count} existing entries for staff {StaffNumber} on {Date}", 
                    refreshed.Count, staffNumber, entryDate);

                openClockCount = 0; inBreak = false;
                foreach (var e in refreshed)
                {
                    var n = ResolveTypeName(e.EntryTypeId);
                    _logger?.LogDebug("  Processing existing entry: EntryId={EntryId}, Type={TypeName}, Timestamp={Timestamp}", 
                        e.EntryId, n, e.EntryTimestamp);
                    
                    switch (n)
                    {
                        case "CLOCK_IN": openClockCount++; break;
                        case "BREAK_START": if (openClockCount > 0) inBreak = true; break;
                        case "BREAK_END": if (inBreak) inBreak = false; break;
                        case "CLOCK_OUT": if (openClockCount > 0) openClockCount--; inBreak = false; break;
                    }
                }

                _logger?.LogInformation("ClockAsync: After state reconstruction: openClockCount={OpenClockCount}, inBreak={InBreak}", 
                    openClockCount, inBreak);

                // Evaluate requested new event (re-run checks)
                _logger?.LogInformation("ClockAsync: Staff={StaffNumber}, RequestedType={TypeName}, openClockCount={Count}, inBreak={InBreak}", 
                    staffNumber, currentTypeName, openClockCount, inBreak);

                switch (currentTypeName)
                {
                    case "CLOCK_IN":
                        if (openClockCount > 0)
                            throw new ArgumentException("Cannot clock in: existing open work session (must clock out first)");
                        if (!_allowMultipleSessionsPerDay)
                        {
                            if (refreshed.Any(e => ResolveTypeName(e.EntryTypeId) == "CLOCK_IN"))
                                throw new ArgumentException("Multiple clock-ins per day are not allowed");
                        }
                        break;
                    case "BREAK_START":
                        if (openClockCount == 0)
                            throw new ArgumentException("Cannot start break before clock in");
                        if (inBreak)
                            throw new ArgumentException("Cannot start break: break already in progress");
                        break;
                    case "BREAK_END":
                        if (openClockCount == 0)
                            throw new ArgumentException("Cannot end break before clock in");
                        if (!inBreak)
                            throw new ArgumentException("No active break to end");
                        break;
                    case "CLOCK_OUT":
                        if (openClockCount == 0)
                        {
                            _logger?.LogWarning("VALIDATION FAILED: Staff={StaffNumber} attempted CLOCK_OUT with openClockCount=0", staffNumber);
                            throw new ArgumentException("Cannot clock out before clock in");
                        }
                        if (inBreak)
                            throw new ArgumentException("Cannot clock out while on break (end break first)");
                        break;
                    default:
                        break;
                }

                var entity = new FarmManagement.Core.Entities.TimeEntry
                {
                    StaffNumber = staffNumber,
                    StationId = dto.StationId,
                    EntryTypeId = dto.EntryTypeId,
                    EntryTimestamp = entryTimestamp,
                    ShiftAssignmentId = resolvedAssignmentId,
                    BreakReason = dto.BreakReason,
                    GeoLocation = dto.GeoLocation,
                    IsManual = dto.IsManual,
                    Status = dto.Status ?? "Open",
                    CreatedAt = System.DateTime.UtcNow
                };

                await _repo.AddAsync(entity, ct);

                // update dto and audits etc (same as before)
                try
                {
                    var et = await _entryTypeRepo.GetByIdAsync(entity.EntryTypeId);
                    if (et != null && string.Equals(et.TypeName, "CLOCK_OUT", StringComparison.OrdinalIgnoreCase) && entity.ShiftAssignmentId.HasValue)
                    {
                        var assignment = await _assignmentRepo.GetByIdAsync(entity.ShiftAssignmentId.Value);
                        if (assignment != null)
                        {
                            assignment.CompletedAt = entity.EntryTimestamp;
                            await _assignmentRepo.UpdateAsync(assignment);

                            var a = new FarmManagement.Core.Entities.AuditLog
                            {
                                TableName = "ShiftAssignments",
                                RecordId = assignment.ShiftAssignmentId,
                                ActionType = "CompleteByClockOut",
                                ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
                                {
                                    Field = "CompletedAt",
                                    Old = (DateTime?)null,
                                    New = assignment.CompletedAt,
                                    Meta = new { CompletedBy = dto.ModifiedBy ?? staffNumber, TimeEntryId = entity.EntryId }
                                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                                PerformedBy = dto.ModifiedBy ?? staffNumber,
                                PerformedAt = DateTime.UtcNow
                            };
                            try { a.CorrelationId = _httpContextAccessor?.HttpContext?.Request?.Headers["X-Correlation-Id"].ToString(); } catch { }
                            await _auditRepo.AddAsync(a, ct);
                        }
                    }
                }
                catch
                {
                    // best-effort
                }

                if (dto.BypassShiftValidation == true)
                {
                    var audit = new FarmManagement.Core.Entities.AuditLog
                    {
                        TableName = "TimeEntries",
                        RecordId = entity.EntryId,
                        ActionType = "BypassShiftValidation",
                        ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            Field = "ShiftValidation",
                            Old = new { Required = true },
                            New = new { Required = false },
                            Meta = new { ShiftAssignmentProvided = dto.ShiftAssignmentId, BypassReason = dto.BypassReason }
                        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                        PerformedBy = dto.ModifiedBy ?? staffNumber,
                        PerformedAt = DateTime.UtcNow
                    };
                    try { audit.CorrelationId = _httpContextAccessor?.HttpContext?.Request?.Headers["X-Correlation-Id"].ToString(); } catch { }
                    await _auditRepo.AddAsync(audit, ct);
                }

                dto.EntryId = entity.EntryId;
                dto.EntryTimestamp = entity.EntryTimestamp;
                dto.CreatedAt = entity.CreatedAt;
                dto.ShiftAssignmentId = entity.ShiftAssignmentId;
                return dto;
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task<IEnumerable<TimeEntryDto>> GetByStaffNumberAsync(string staffNumber, CancellationToken ct = default)
        {
            var list = await _repo.GetByStaffNumberAsync(staffNumber, ct);
            return list.Select(e => new TimeEntryDto
            {
                EntryId = e.EntryId,
                StaffNumber = e.StaffNumber,
                StationId = e.StationId,
                ShiftAssignmentId = e.ShiftAssignmentId,
                EntryTypeId = e.EntryTypeId,
                EntryTimestamp = e.EntryTimestamp,
                BreakReason = e.BreakReason,
                GeoLocation = e.GeoLocation,
                IsManual = e.IsManual,
                Status = e.Status
            });
        }

        public async Task<IEnumerable<TimeEntryDto>> GetTodayEntriesAsync(string staffNumber, CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(System.DateTime.UtcNow);
            var list = await _repo.GetByStaffNumberAndDateAsync(staffNumber, today, ct);
            return list.Select(e => new TimeEntryDto
            {
                EntryId = e.EntryId,
                StaffNumber = e.StaffNumber,
                StationId = e.StationId,
                ShiftAssignmentId = e.ShiftAssignmentId,
                EntryTypeId = e.EntryTypeId,
                EntryTimestamp = e.EntryTimestamp,
                BreakReason = e.BreakReason,
                GeoLocation = e.GeoLocation,
                IsManual = e.IsManual,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                ModifiedAt = e.ModifiedAt,
                ModifiedBy = e.ModifiedBy,
                ModifiedReason = e.ModifiedReason
            });
        }

        // Consolidated ManualEditAsync with auditing that explicitly records the TimeEntryId being fixed
        public async Task<TimeEntryDto> ManualEditAsync(int entryId, TimeEntryDto dto, CancellationToken ct = default)
        {
            var existing = await _repo.GetByIdAsync(entryId, ct);
            if (existing == null) throw new System.ArgumentException("Time entry not found");

            // Capture before state for auditing
            var beforeTimestamp = existing.EntryTimestamp;
            var beforeBreakReason = existing.BreakReason;
            var beforeGeoLocation = existing.GeoLocation;
            var beforeIsManual = existing.IsManual;
            var beforeStatus = existing.Status;
            var beforeModifiedBy = existing.ModifiedBy;
            var beforeModifiedReason = existing.ModifiedReason;

            // Apply editable fields
            existing.EntryTimestamp = dto.EntryTimestamp;
            existing.BreakReason = dto.BreakReason;
            existing.GeoLocation = dto.GeoLocation;
            existing.IsManual = dto.IsManual;
            existing.ModifiedAt = dto.ModifiedAt ?? System.DateTime.UtcNow;
            existing.ModifiedBy = dto.ModifiedBy;
            existing.ModifiedReason = dto.ModifiedReason;
            existing.Status = dto.Status ?? existing.Status;

            await _repo.UpdateAsync(existing, ct);

            // Build audit changes
            var changes = new List<object>();

            if (beforeTimestamp != existing.EntryTimestamp)
                changes.Add(new { Field = "EntryTimestamp", Old = (object)beforeTimestamp, New = (object)existing.EntryTimestamp });
            if (!string.Equals(beforeBreakReason, existing.BreakReason, StringComparison.Ordinal))
                changes.Add(new { Field = "BreakReason", Old = (object?)beforeBreakReason, New = (object?)existing.BreakReason });
            if (!string.Equals(beforeGeoLocation, existing.GeoLocation, StringComparison.Ordinal))
                changes.Add(new { Field = "GeoLocation", Old = (object?)beforeGeoLocation, New = (object?)existing.GeoLocation });
            if (beforeIsManual != existing.IsManual)
                changes.Add(new { Field = "IsManual", Old = (object)beforeIsManual, New = (object)existing.IsManual });
            if (!string.Equals(beforeStatus, existing.Status, StringComparison.Ordinal))
                changes.Add(new { Field = "Status", Old = (object?)beforeStatus, New = (object?)existing.Status });
            if (!string.Equals(beforeModifiedBy, existing.ModifiedBy, StringComparison.Ordinal))
                changes.Add(new { Field = "ModifiedBy", Old = (object?)beforeModifiedBy, New = (object?)existing.ModifiedBy });
            if (!string.Equals(beforeModifiedReason, existing.ModifiedReason, StringComparison.Ordinal))
                changes.Add(new { Field = "ModifiedReason", Old = (object?)beforeModifiedReason, New = (object?)existing.ModifiedReason });

            // Always record audit for the manual edit and include the TimeEntryId explicitly
            var editAudit = new FarmManagement.Core.Entities.AuditLog
            {
                TableName = "TimeEntries",
                RecordId = existing.EntryId,
                ActionType = "ManualEdit",
                ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Fields = changes,
                    Meta = new
                    {
                        TimeEntryId = existing.EntryId,
                        StaffNumber = existing.StaffNumber,
                        ShiftAssignmentId = existing.ShiftAssignmentId,
                        EditedAt = existing.ModifiedAt,
                        EditedBy = existing.ModifiedBy,
                        Reason = existing.ModifiedReason
                    }
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                PerformedBy = existing.ModifiedBy ?? dto.ModifiedBy ?? existing.StaffNumber ?? "System",
                PerformedAt = System.DateTime.UtcNow
            };
            try { editAudit.CorrelationId = _httpContextAccessor?.HttpContext?.Request?.Headers["X-Correlation-Id"].ToString(); } catch { }
            await _auditRepo.AddAsync(editAudit, ct);

            dto.EntryId = existing.EntryId;
            dto.CreatedAt = existing.CreatedAt;
            dto.ShiftAssignmentId = existing.ShiftAssignmentId;
            return dto;
        }

        public async Task<ExceptionDto> CreateExceptionAsync(ExceptionDto dto, CancellationToken ct = default)
        {
            var log = new FarmManagement.Core.Entities.ExceptionLog
            {
                StaffNumber = dto.StaffNumber,
                ExceptionDate = dto.ExceptionDate,
                TypeId = dto.TypeId,
                Description = dto.Description,
                Status = dto.Status,
                ResolutionNotes = dto.ResolutionNotes,
                ResolvedBy = dto.ResolvedBy,
                CreatedAt = System.DateTime.UtcNow,
                ResolvedAt = dto.ResolvedAt
            };

            await _excRepo.AddAsync(log, ct);
            dto.ExceptionId = log.ExceptionId;
            dto.CreatedAt = log.CreatedAt;
            try
            {
                // Create audit log for creation
                var audit = new FarmManagement.Core.Entities.AuditLog
                {
                    TableName = "ExceptionLogs",
                    RecordId = log.ExceptionId,
                    ActionType = "Create",
                    ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Fields = new object[] {
                            new { Field = "StaffNumber", Old = (object?)null, New = (object?)log.StaffNumber },
                            new { Field = "ExceptionDate", Old = (object?)null, New = (object?)log.ExceptionDate },
                            new { Field = "TypeId", Old = (object?)null, New = (object?)log.TypeId },
                            new { Field = "Description", Old = (object?)null, New = (object?)log.Description },
                            new { Field = "Status", Old = (object?)null, New = (object?)log.Status }
                        }
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                    PerformedBy = dto.StaffNumber ?? "System",
                    PerformedAt = System.DateTime.UtcNow
                };

                try { audit.CorrelationId = _httpContextAccessor?.HttpContext?.Request?.Headers["X-Correlation-Id"].ToString(); } catch { }
                await _auditRepo.AddAsync(audit, ct);

                // Structured log
                _logger?.LogInformation("Created exception {ExceptionId} for staff {StaffNumber} (Type {TypeId})", log.ExceptionId, log.StaffNumber, log.TypeId);
            }
            catch (Exception ex)
            {
                // Best-effort: do not fail the create if audit fails, but log error
                _logger?.LogError(ex, "Failed to write audit for created exception for staff {StaffNumber}", dto.StaffNumber);
            }
            return dto;
        }

        // Admin: edit an entire session for a staff and date in one request (clock in/out + multiple breaks)
        public async Task<StaffSessionDto> ManualEditSessionAsync(string staffNumber, DateOnly date, ManualSessionEditRequest request, string performedBy, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.ModifiedReason))
                throw new ArgumentException("ModifiedReason is required");

            var sn = (staffNumber ?? string.Empty).Trim();
            var dayStart = date.ToDateTime(new TimeOnly(0));
            var dayEnd = date.ToDateTime(new TimeOnly(23, 59, 59, 999));

            // Basic validations
            if (request.ClockIn.HasValue && request.ClockIn.Value < dayStart)
                throw new ArgumentException("ClockIn must be within the specified date");
            if (request.ClockOut.HasValue && request.ClockOut.Value > dayEnd)
                throw new ArgumentException("ClockOut must be within the specified date");
            if (request.ClockIn.HasValue && request.ClockOut.HasValue && request.ClockOut < request.ClockIn)
                throw new ArgumentException("ClockOut cannot be earlier than ClockIn");

            // Validate breaks
            var breaks = (request.Breaks ?? new List<BreakEditDto>()).OrderBy(b => b.Start).ToList();
            foreach (var b in breaks)
            {
                if (b.Start < dayStart || b.Start > dayEnd)
                    throw new ArgumentException("Break start must be within the specified date");
                if (b.End.HasValue)
                {
                    if (b.End.Value < b.Start) throw new ArgumentException("Break end cannot be earlier than break start");
                    if (b.End.Value < dayStart || b.End.Value > dayEnd)
                        throw new ArgumentException("Break end must be within the specified date");
                }
            }
            // Ensure no overlapping breaks
            for (int i = 1; i < breaks.Count; i++)
            {
                var prev = breaks[i - 1];
                var curr = breaks[i];
                var prevEnd = prev.End ?? prev.Start; // open break treated as zero-length here for overlap check
                if (prevEnd > curr.Start)
                    throw new ArgumentException("Break intervals must not overlap");
            }

            // Ensure breaks fit within [clockIn, clockOut] when both provided
            if (request.ClockIn.HasValue && request.ClockOut.HasValue)
            {
                foreach (var b in breaks)
                {
                    if (b.Start < request.ClockIn.Value || b.Start > request.ClockOut.Value)
                        throw new ArgumentException("Break start must be within the work session time range");
                    if (b.End.HasValue && (b.End.Value < request.ClockIn.Value || b.End.Value > request.ClockOut.Value))
                        throw new ArgumentException("Break end must be within the work session time range");
                }
            }

            // Resolve EntryTypeIds
            var clockInType = await _entryTypeRepo.GetByNameAsync("CLOCK_IN") ?? throw new ArgumentException("EntryType CLOCK_IN not found");
            var breakStartType = await _entryTypeRepo.GetByNameAsync("BREAK_START") ?? throw new ArgumentException("EntryType BREAK_START not found");
            var breakEndType = await _entryTypeRepo.GetByNameAsync("BREAK_END") ?? throw new ArgumentException("EntryType BREAK_END not found");
            var clockOutType = await _entryTypeRepo.GetByNameAsync("CLOCK_OUT") ?? throw new ArgumentException("EntryType CLOCK_OUT not found");

            // Load existing entries for date with tracking
            var existing = await _repo.GetByStaffNumberAndDateForUpdateAsync(sn, date, ct);
            // Build desired entries list
            var desired = new List<Core.Entities.TimeEntry>();
            int stationId = request.StationId ?? existing.FirstOrDefault()?.StationId ?? 0;
            if (stationId == 0)
                throw new ArgumentException("StationId is required when creating new entries and cannot be inferred");

            int? shiftAssignmentId = request.ShiftAssignmentId ?? existing.FirstOrDefault()?.ShiftAssignmentId;
            DateTime modAt = request.ModifiedAt ?? DateTime.UtcNow;

            if (request.ClockIn.HasValue)
            {
                desired.Add(new Core.Entities.TimeEntry
                {
                    StaffNumber = sn,
                    StationId = stationId,
                    ShiftAssignmentId = shiftAssignmentId,
                    EntryTypeId = clockInType.EntryTypeId,
                    EntryTimestamp = request.ClockIn.Value,
                    IsManual = request.IsManual,
                    ModifiedBy = performedBy,
                    ModifiedReason = request.ModifiedReason,
                    ModifiedAt = modAt,
                    Status = request.Status
                });
            }
            foreach (var b in breaks)
            {
                desired.Add(new Core.Entities.TimeEntry
                {
                    StaffNumber = sn,
                    StationId = stationId,
                    ShiftAssignmentId = shiftAssignmentId,
                    EntryTypeId = breakStartType.EntryTypeId,
                    EntryTimestamp = b.Start,
                    IsManual = request.IsManual,
                    ModifiedBy = performedBy,
                    ModifiedReason = request.ModifiedReason,
                    ModifiedAt = modAt,
                    Status = request.Status
                });
                if (b.End.HasValue)
                {
                    desired.Add(new Core.Entities.TimeEntry
                    {
                        StaffNumber = sn,
                        StationId = stationId,
                        ShiftAssignmentId = shiftAssignmentId,
                        EntryTypeId = breakEndType.EntryTypeId,
                        EntryTimestamp = b.End.Value,
                        IsManual = request.IsManual,
                        ModifiedBy = performedBy,
                        ModifiedReason = request.ModifiedReason,
                        ModifiedAt = modAt,
                        Status = request.Status
                    });
                }
            }
            if (request.ClockOut.HasValue)
            {
                desired.Add(new Core.Entities.TimeEntry
                {
                    StaffNumber = sn,
                    StationId = stationId,
                    ShiftAssignmentId = shiftAssignmentId,
                    EntryTypeId = clockOutType.EntryTypeId,
                    EntryTimestamp = request.ClockOut.Value,
                    IsManual = request.IsManual,
                    ModifiedBy = performedBy,
                    ModifiedReason = request.ModifiedReason,
                    ModifiedAt = modAt,
                    Status = request.Status
                });
            }

            // Order desired chronologically and by type ordering within same timestamp for determinism
            int OrderKey(int entryTypeId) => entryTypeId == clockInType.EntryTypeId ? 0 : entryTypeId == breakStartType.EntryTypeId ? 1 : entryTypeId == breakEndType.EntryTypeId ? 2 : 3;
            desired = desired.OrderBy(d => d.EntryTimestamp).ThenBy(d => OrderKey(d.EntryTypeId)).ToList();

            // Group existing by type occurrence order to align updates
            var existingOrdered = existing.OrderBy(e => e.EntryTimestamp).ThenBy(e => OrderKey(e.EntryTypeId)).ToList();

            var toUpdate = new List<(Core.Entities.TimeEntry existing, Core.Entities.TimeEntry desired)>();
            var toAdd = new List<Core.Entities.TimeEntry>();
            var toDelete = new List<Core.Entities.TimeEntry>();

            int minCount = Math.Min(existingOrdered.Count, desired.Count);
            for (int i = 0; i < minCount; i++)
            {
                var ex = existingOrdered[i];
                var de = desired[i];
                // if types differ, better to replace by delete+add for correctness
                if (ex.EntryTypeId == de.EntryTypeId)
                    toUpdate.Add((ex, de));
                else
                {
                    toDelete.Add(ex);
                    toAdd.Add(de);
                }
            }
            if (desired.Count > existingOrdered.Count)
            {
                toAdd.AddRange(desired.Skip(existingOrdered.Count));
            }
            else if (existingOrdered.Count > desired.Count)
            {
                toDelete.AddRange(existingOrdered.Skip(desired.Count));
            }

            await _repo.BeginTransactionAsync(ct);
            try
            {
                // Apply updates
                foreach (var (ex, de) in toUpdate)
                {
                    ex.EntryTimestamp = de.EntryTimestamp;
                    ex.StationId = de.StationId;
                    ex.ShiftAssignmentId = de.ShiftAssignmentId;
                    ex.IsManual = de.IsManual;
                    ex.ModifiedBy = de.ModifiedBy;
                    ex.ModifiedReason = de.ModifiedReason;
                    ex.ModifiedAt = de.ModifiedAt;
                    ex.Status = de.Status ?? ex.Status;
                }
                if (toUpdate.Count > 0)
                {
                    // EF tracked entities: Save via one SaveChanges on UpdateAsync of any entity
                    foreach (var u in toUpdate)
                    {
                        await _repo.UpdateAsync(u.existing, ct);
                    }
                }

                if (toAdd.Count > 0)
                {
                    await _repo.AddRangeAsync(toAdd, ct);
                }

                if (toDelete.Count > 0)
                {
                    await _repo.DeleteRangeAsync(toDelete, ct);
                }

                // Build audit payload
                var auditPayload = new
                {
                    Action = "ManualSessionEdit",
                    StaffNumber = sn,
                    Date = date,
                    ModifiedBy = performedBy,
                    ModifiedAt = modAt,
                    ModifiedReason = request.ModifiedReason,
                    Added = toAdd.Select(a => new { a.EntryTypeId, a.EntryTimestamp, a.StationId, a.ShiftAssignmentId }).ToList(),
                    Updated = toUpdate.Select(u => new { EntryId = u.existing.EntryId, OldTimestamp = u.existing.EntryTimestamp, NewTimestamp = u.desired.EntryTimestamp, u.existing.EntryTypeId }).ToList(),
                    Deleted = toDelete.Select(d => new { EntryId = d.EntryId, d.EntryTypeId, d.EntryTimestamp }).ToList()
                };

                var audit = new FarmManagement.Core.Entities.AuditLog
                {
                    TableName = "TimeEntries",
                    RecordId = 0, // session-level; individual changes listed in payload
                    ActionType = "ManualSessionEdit",
                    ChangesJson = System.Text.Json.JsonSerializer.Serialize(auditPayload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                    PerformedBy = performedBy,
                    PerformedAt = DateTime.UtcNow
                };
                try { audit.CorrelationId = _httpContextAccessor?.HttpContext?.Request?.Headers["X-Correlation-Id"].ToString(); } catch { }
                await _auditRepo.AddAsync(audit, ct);

                await _repo.CommitTransactionAsync(ct);
            }
            catch
            {
                await _repo.RollbackTransactionAsync(ct);
                throw;
            }

            // Build response session from desired shapes
            var session = new StaffSessionDto
            {
                StaffNumber = sn,
                Date = date,
                ClockIn = request.ClockIn,
                ClockOut = request.ClockOut,
                Breaks = breaks.Select(b => new BreakIntervalDto { Start = b.Start, End = b.End }).ToList()
            };
            if (session.Breaks.Count > 0)
            {
                session.BreakStart = session.Breaks[0].Start;
                session.BreakEnd = session.Breaks[0].End;
            }

            // Compute derived fields using existing helper if available
            try
            {
                var computeMethod = this.GetType().GetMethod("ComputeDerivedDurations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                computeMethod?.Invoke(this, new object[] { session });
            }
            catch { }

            return session;
        }

        public async Task<IEnumerable<ExceptionDto>> GetExceptionsAsync(string staffNumber, DateOnly date, CancellationToken ct = default)
        {
            var logs = await _excRepo.GetByStaffNumberAndDateAsync(staffNumber, date, ct);
            return logs.Select(l => new ExceptionDto
            {
                ExceptionId = l.ExceptionId,
                StaffNumber = l.StaffNumber,
                ExceptionDate = l.ExceptionDate,
                TypeId = l.TypeId,
                Description = l.Description,
                Status = l.Status,
                ResolutionNotes = l.ResolutionNotes,
                ResolvedBy = l.ResolvedBy,
                CreatedAt = l.CreatedAt,
                ResolvedAt = l.ResolvedAt
            });
        }

        public async Task<ExceptionDto> ResolveExceptionAsync(int exceptionId, string resolvedBy, string resolutionNotes, CancellationToken ct = default)
        {
            var existing = await _excRepo.GetByIdAsync(exceptionId, ct);
            if (existing == null) throw new System.ArgumentException("Exception not found");

            // Update fields
            existing.Status = "Resolved";
            existing.ResolutionNotes = resolutionNotes;
            existing.ResolvedBy = resolvedBy;
            existing.ResolvedAt = System.DateTime.UtcNow;

            await _excRepo.UpdateAsync(existing, ct);

            // Create audit log
            var audit = new FarmManagement.Core.Entities.AuditLog
            {
                TableName = "ExceptionLogs",
                RecordId = existing.ExceptionId,
                ActionType = "Resolve",
                ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Fields = new object[] {
                        new { Field = "Status", Old = (object?)"Open", New = (object?)existing.Status },
                        new { Field = "ResolutionNotes", Old = (object?)null, New = (object?)existing.ResolutionNotes },
                        new { Field = "ResolvedBy", Old = (object?)null, New = (object?)existing.ResolvedBy },
                        new { Field = "ResolvedAt", Old = (object?)null, New = (object?)existing.ResolvedAt }
                    }
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                PerformedBy = resolvedBy,
                PerformedAt = System.DateTime.UtcNow
            };

            try { audit.CorrelationId = _httpContextAccessor?.HttpContext?.Request?.Headers["X-Correlation-Id"].ToString(); } catch { }
            await _auditRepo.AddAsync(audit, ct);

            // Structured log
            _logger?.LogInformation("Resolved exception {ExceptionId} for staff {StaffNumber} by {ResolvedBy}", existing.ExceptionId, existing.StaffNumber, resolvedBy);

            return new ExceptionDto
            {
                ExceptionId = existing.ExceptionId,
                StaffNumber = existing.StaffNumber,
                ExceptionDate = existing.ExceptionDate,
                TypeId = existing.TypeId,
                Description = existing.Description,
                Status = existing.Status,
                ResolutionNotes = existing.ResolutionNotes,
                ResolvedBy = existing.ResolvedBy,
                CreatedAt = existing.CreatedAt,
                ResolvedAt = existing.ResolvedAt
            };
        }

        public async Task<FarmManagement.Application.DTOs.PagedResult<TimeEntryDto>> QueryAsync(string? staffNumber = null, int? entryTypeId = null, DateTime? start = null, DateTime? end = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var paged = await _repo.QueryAsync(staffNumber, entryTypeId, start, end, page, pageSize, ct);
            var dtoItems = paged.Items.Select(e => new TimeEntryDto
            {
                EntryId = e.EntryId,
                StaffNumber = e.StaffNumber,
                StationId = e.StationId,
                ShiftAssignmentId = e.ShiftAssignmentId,
                EntryTypeId = e.EntryTypeId,
                EntryTimestamp = e.EntryTimestamp,
                BreakReason = e.BreakReason,
                GeoLocation = e.GeoLocation,
                IsManual = e.IsManual,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                ModifiedAt = e.ModifiedAt,
                ModifiedBy = e.ModifiedBy,
                ModifiedReason = e.ModifiedReason
            }).ToList();

            return new FarmManagement.Application.DTOs.PagedResult<TimeEntryDto>
            {
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                Items = dtoItems
            };
        }

        // GetEntriesWithAuditsAsync removed: prefer Audit API / per-entry audit endpoints to avoid large combined payloads
        public async Task<IEnumerable<StaffSessionDto>> GetSessionsAsync(string staffNumber, DateOnly? date = null, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(staffNumber)) throw new ArgumentException("staffNumber is required");

            // Determine date(s)
            List<DateOnly> dates = new();
            if (date.HasValue)
            {
                dates.Add(date.Value);
            }
            else
            {
                var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var end = endDate ?? start;
                if (end < start) (start, end) = (end, start);
                for (var d = start; d <= end; d = d.AddDays(1)) dates.Add(d);
            }

            var results = new List<StaffSessionDto>();

            foreach (var d in dates)
            {
                var entries = (await _repo.GetByStaffNumberAndDateAsync(staffNumber.Trim(), d, ct))
                    .OrderBy(e => e.EntryTimestamp)
                    .ToList();

                // Map entry type Ids to names
                var typeIds = entries.Select(e => e.EntryTypeId).Distinct().ToList();
                var typeNames = new Dictionary<int, string>();
                foreach (var id in typeIds)
                {
                    try
                    {
                        var et = await _entryTypeRepo.GetByIdAsync(id);
                        typeNames[id] = (et?.TypeName ?? string.Empty).Trim().ToUpperInvariant();
                    }
                    catch { typeNames[id] = string.Empty; }
                }

                // Build multiple sessions for this date
                StaffSessionDto? current = null;
                bool inBreak = false;
                DateTime? pendingBreakStart = null;

                foreach (var e in entries)
                {
                    var t = typeNames.TryGetValue(e.EntryTypeId, out var n) ? n : string.Empty;
                    switch (t)
                    {
                        case "CLOCK_IN":
                            if (current == null)
                            {
                                current = new StaffSessionDto { StaffNumber = staffNumber.Trim(), Date = d, ClockIn = e.EntryTimestamp };
                                inBreak = false;
                            }
                            else
                            {
                                // Unexpected extra CLOCK_IN without CLOCK_OUT: finalize previous as-is and start a new one
                                results.Add(current);
                                current = new StaffSessionDto { StaffNumber = staffNumber.Trim(), Date = d, ClockIn = e.EntryTimestamp };
                                inBreak = false;
                            }
                            break;
                        case "BREAK_START":
                            if (current != null && !inBreak)
                            {
                                // First break for back-compat fields
                                if (current.BreakStart == null)
                                {
                                    current.BreakStart = e.EntryTimestamp;
                                }
                                pendingBreakStart = e.EntryTimestamp;
                                inBreak = true;
                            }
                            break;
                        case "BREAK_END":
                            if (current != null && inBreak)
                            {
                                // First break for back-compat fields
                                if (current.BreakEnd == null)
                                {
                                    current.BreakEnd = e.EntryTimestamp;
                                }
                                // Add to breaks list
                                if (pendingBreakStart.HasValue)
                                {
                                    current.Breaks.Add(new BreakIntervalDto { Start = pendingBreakStart.Value, End = e.EntryTimestamp });
                                }
                                pendingBreakStart = null;
                                inBreak = false;
                            }
                            break;
                        case "CLOCK_OUT":
                            if (current != null)
                            {
                                current.ClockOut = e.EntryTimestamp;
                                // If break was started but not ended by clock-out, close it as open-ended
                                if (inBreak && pendingBreakStart.HasValue)
                                {
                                    current.Breaks.Add(new BreakIntervalDto { Start = pendingBreakStart.Value, End = null });
                                }
                                ComputeDerivedDurations(current);
                                results.Add(current);
                                current = null;
                                inBreak = false;
                                pendingBreakStart = null;
                            }
                            break;
                    }
                }

                // If a session is in progress (no CLOCK_OUT), include it as-is
                if (current != null)
                {
                    if (inBreak && pendingBreakStart.HasValue)
                    {
                        current.Breaks.Add(new BreakIntervalDto { Start = pendingBreakStart.Value, End = null });
                    }
                    // compute derived fields
                    ComputeDerivedDurations(current);
                    results.Add(current);
                }
            }

            return results;
        }

        private static void ComputeDerivedDurations(StaffSessionDto session)
        {
            // Calculate total break minutes: sum of closed intervals only (ignore open-ended breaks)
            double totalBreakMinutes = 0;
            foreach (var b in session.Breaks)
            {
                if (b.End.HasValue)
                {
                    var diff = (b.End.Value - b.Start).TotalMinutes;
                    if (diff > 0) totalBreakMinutes += diff;
                }
            }
            session.TotalBreakMinutes = (int)Math.Round(totalBreakMinutes);

            // Worked minutes: if both ClockIn and ClockOut exist, subtract total break minutes
            if (session.ClockIn.HasValue && session.ClockOut.HasValue)
            {
                var total = (session.ClockOut.Value - session.ClockIn.Value).TotalMinutes;
                var worked = total - totalBreakMinutes;
                if (worked < 0) worked = 0; // guard
                session.WorkedMinutes = (int)Math.Round(worked);
            }
            else
            {
                session.WorkedMinutes = null; // in-progress session
            }
        }

    public async Task<PagedResult<StaffSessionDto>> GetAllStaffSessionsAsync(DateOnly? date = null, DateOnly? startDate = null, DateOnly? endDate = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            // Resolve date range: if no filters provided, load ALL entries (no date restriction)
            IEnumerable<Core.Entities.TimeEntry> entries;
            if (date.HasValue)
            {
                var start = date.Value;
                var end = date.Value;
                entries = await _repo.GetByDateRangeAsync(start, end, ct);
            }
            else if (startDate.HasValue || endDate.HasValue)
            {
                var start = startDate ?? DateOnly.MinValue;
                var end = endDate ?? DateOnly.MaxValue;
                if (end < start) (start, end) = (end, start);
                entries = await _repo.GetByDateRangeAsync(start, end, ct);
            }
            else
            {
                // No date filters: return all time entries
                // We'll need a repository method to get all entries; for now use a very wide range
                var start = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)); // practical "all" range
                var end = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
                entries = await _repo.GetByDateRangeAsync(start, end, ct);
            }

            var entriesList = entries
                .OrderBy(e => e.StaffNumber)
                .ThenBy(e => e.EntryTimestamp)
                .ToList();

            // Cache entry type names
            var typeIds = entriesList.Select(e => e.EntryTypeId).Distinct().ToList();
            var typeNames = new Dictionary<int, string>();
            foreach (var id in typeIds)
            {
                try
                {
                    var et = await _entryTypeRepo.GetByIdAsync(id);
                    typeNames[id] = (et?.TypeName ?? string.Empty).Trim().ToUpperInvariant();
                }
                catch { typeNames[id] = string.Empty; }
            }

            var grouped = entriesList.GroupBy(e => new { e.StaffNumber, Date = DateOnly.FromDateTime(e.EntryTimestamp) });
            var list = new List<StaffSessionDto>();

            foreach (var g in grouped)
            {
                StaffSessionDto? current = null;
                bool inBreak = false;
                DateTime? pendingBreakStart = null;

                foreach (var e in g)
                {
                    var t = typeNames.TryGetValue(e.EntryTypeId, out var n) ? n : string.Empty;
                    switch (t)
                    {
                        case "CLOCK_IN":
                            if (current == null)
                            {
                                current = new StaffSessionDto { StaffNumber = g.Key.StaffNumber, Date = g.Key.Date, ClockIn = e.EntryTimestamp };
                                inBreak = false; pendingBreakStart = null;
                            }
                            else
                            {
                                // finalize previous incomplete session and start a new one
                                if (inBreak && pendingBreakStart.HasValue)
                                {
                                    current.Breaks.Add(new BreakIntervalDto { Start = pendingBreakStart.Value, End = null });
                                }
                                ComputeDerivedDurations(current);
                                list.Add(current);
                                current = new StaffSessionDto { StaffNumber = g.Key.StaffNumber, Date = g.Key.Date, ClockIn = e.EntryTimestamp };
                                inBreak = false; pendingBreakStart = null;
                            }
                            break;
                        case "BREAK_START":
                            if (current != null && !inBreak)
                            {
                                if (current.BreakStart == null) current.BreakStart = e.EntryTimestamp; // back-compat
                                pendingBreakStart = e.EntryTimestamp;
                                inBreak = true;
                            }
                            break;
                        case "BREAK_END":
                            if (current != null && inBreak)
                            {
                                if (current.BreakEnd == null) current.BreakEnd = e.EntryTimestamp; // back-compat
                                if (pendingBreakStart.HasValue)
                                {
                                    current.Breaks.Add(new BreakIntervalDto { Start = pendingBreakStart.Value, End = e.EntryTimestamp });
                                }
                                pendingBreakStart = null;
                                inBreak = false;
                            }
                            break;
                        case "CLOCK_OUT":
                            if (current != null)
                            {
                                current.ClockOut = e.EntryTimestamp;
                                if (inBreak && pendingBreakStart.HasValue)
                                {
                                    current.Breaks.Add(new BreakIntervalDto { Start = pendingBreakStart.Value, End = null });
                                }
                                ComputeDerivedDurations(current);
                                list.Add(current);
                                current = null; inBreak = false; pendingBreakStart = null;
                            }
                            break;
                    }
                }

                if (current != null)
                {
                    if (inBreak && pendingBreakStart.HasValue)
                    {
                        current.Breaks.Add(new BreakIntervalDto { Start = pendingBreakStart.Value, End = null });
                    }
                    ComputeDerivedDurations(current);
                    list.Add(current);
                }
            }

            var ordered = list.OrderBy(s => s.StaffNumber).ThenBy(s => s.Date).ToList();
            var total = ordered.LongCount();
            // basic bounds
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResult<StaffSessionDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };
        }
    }
}