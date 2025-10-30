using System;

namespace FarmManagement.Application.DTOs
{
    public class ShiftTypeDto
    {
        public int ShiftTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeSpan DefaultStartTime { get; set; }
        public TimeSpan DefaultEndTime { get; set; }
        public string? Description { get; set; }
    }
}
