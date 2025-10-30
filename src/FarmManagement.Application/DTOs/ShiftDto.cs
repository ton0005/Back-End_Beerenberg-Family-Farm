using System;

namespace FarmManagement.Application.DTOs
{
    public class ShiftDto
    {
        public int ShiftId { get; set; }
        public int ShiftTypeId { get; set; }
        // Accept shift type as name (preferred) or by id. If both provided, name takes precedence.
        public string ShiftTypeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public TimeSpan? Break { get; set; }
        public string? Note { get; set; }
        public bool IsPublished { get; set; }
        public IReadOnlyCollection<ShiftAssignmentDto> Assignments { get; set; } = [];
    }
}
