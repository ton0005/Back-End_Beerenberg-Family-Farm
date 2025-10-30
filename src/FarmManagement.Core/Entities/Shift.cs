using System;

namespace FarmManagement.Core.Entities
{
    public class Shift
    {
        public int ShiftId { get; set; }

        // Reference to the template/type
        public int ShiftTypeId { get; set; }
        public ShiftType? ShiftType { get; set; }

        // Date of the shift (date portion matters) and optional overrides for times
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        // Optional note/description
        public string? Note { get; set; }

        // Break duration during the shift (optional)
        public TimeSpan? Break { get; set; }

        // Whether the shift has been published (visible to staff). Defaults to false (draft).
        public bool IsPublished { get; set; } = false;
    }
}
