using System;

namespace FarmManagement.Core.Entities
{
    public class AuditLog
    {
        public int AuditId { get; set; }
        public string? TableName { get; set; }
        public int RecordId { get; set; }
        public string? ActionType { get; set; }
        public string? ChangesJson { get; set; }
        // Optional correlation id to tie audits back to a request/response
        public string? CorrelationId { get; set; }
        public string? PerformedBy { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    }
}
