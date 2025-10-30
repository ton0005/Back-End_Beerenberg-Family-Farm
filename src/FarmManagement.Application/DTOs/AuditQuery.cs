namespace FarmManagement.Application.DTOs;

public class AuditQuery
{
    public string? TableName { get; set; }
    public int[]? RecordIds { get; set; }
    public string? CorrelationId { get; set; }
    public string? ActionType { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
