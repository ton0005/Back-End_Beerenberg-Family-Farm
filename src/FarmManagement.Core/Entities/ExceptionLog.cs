using System;

namespace FarmManagement.Core.Entities
{
    public class ExceptionLog
    {
        public int ExceptionId { get; set; }
        public string StaffNumber { get; set; } = string.Empty;
        public DateOnly ExceptionDate { get; set; }
        public int TypeId { get; set; }
        public ExceptionType? Type { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? ResolutionNotes { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
