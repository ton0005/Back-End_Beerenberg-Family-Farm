namespace FarmManagement.Application.DTOs;

public class AuditDto
{
    public int AuditId { get; set; }
    public string? TableName { get; set; }
    public int RecordId { get; set; }
    public string? ActionType { get; set; }
    public string? ChangesJson { get; set; }
    public string? PerformedBy { get; set; }
    public System.DateTime PerformedAt { get; set; }
}
