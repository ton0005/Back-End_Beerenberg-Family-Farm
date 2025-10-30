using System;

namespace FarmManagement.Application.DTOs
{
    public class TimeEntryDto
    {
        public int EntryId { get; set; }
        public string StaffNumber { get; set; } = string.Empty;
        public int StationId { get; set; }
    public int? ShiftAssignmentId { get; set; }
    // If true, skip shift-assignment validation (admin-only). Controller must enforce admin rights.
    public bool? BypassShiftValidation { get; set; }
        // Optional admin-provided reason when bypassing shift validation. Stored in audit log if bypass used.
        public string? BypassReason { get; set; }
        public int EntryTypeId { get; set; }
        public DateTime EntryTimestamp { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public string? ModifiedReason { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? BreakReason { get; set; }
        public string? GeoLocation { get; set; }
        public bool IsManual { get; set; }
        public string? Status { get; set; }
    }
}
