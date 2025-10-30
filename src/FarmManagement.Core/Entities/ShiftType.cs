using System;

namespace FarmManagement.Core.Entities
{
    // Lookup table for reusable shift templates (e.g. Morning, Afternoon)
    public class ShiftType
    {
        public int ShiftTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeSpan DefaultStartTime { get; set; }
        public TimeSpan DefaultEndTime { get; set; }
        public string? Description { get; set; }
    }
}
