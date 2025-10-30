using System;

namespace FarmManagement.Application.DTOs
{
    public class PublicShiftDto
    {
        public int ShiftId { get; set; }
        public string? ShiftType { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public TimeSpan? Break { get; set; }
        public string? Status { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
