using System;

namespace FarmManagement.Core.Entities
{
    public class TimeStation
    {
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
