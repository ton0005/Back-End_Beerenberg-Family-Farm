using System;

namespace FarmManagement.Core.Entities
{
    public class TimeEntry
    {
        public int EntryId { get; set; }
        public string StaffNumber { get; set; } = string.Empty; // FK to Staff.StaffNumber
        public int StationId { get; set; }
        public TimeStation? Station { get; set; }
    public int? ShiftAssignmentId { get; set; }
        public int EntryTypeId { get; set; }
        public EntryType? EntryType { get; set; }
        public DateTime EntryTimestamp { get; set; }
        public string? BreakReason { get; set; }
        public string? GeoLocation { get; set; }
        public bool IsManual { get; set; }
        public string? ModifiedBy { get; set; }
        public string? ModifiedReason { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
