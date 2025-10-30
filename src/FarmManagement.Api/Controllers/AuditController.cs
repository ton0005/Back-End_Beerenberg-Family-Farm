using FarmManagement.Application.DTOs;
using FarmManagement.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditRepository _auditRepo;

        public AuditController(IAuditRepository auditRepo)
        {
            _auditRepo = auditRepo;
        }

    /// <summary>
    /// Query audits with filters and paging. Requires role Admin or Auditor.
    /// Example: GET /api/audit?tableName=TimeEntries&amp;recordIds=1,2&amp;correlationId=...&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Query([FromQuery] string? tableName, [FromQuery] string? recordIds, [FromQuery] string? correlationId,
            [FromQuery] string? actionType, [FromQuery] string? performedBy, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = new AuditQuery
            {
                TableName = tableName,
                CorrelationId = correlationId,
                ActionType = actionType,
                PerformedBy = performedBy,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize
            };

            if (!string.IsNullOrWhiteSpace(recordIds))
            {
                query.RecordIds = recordIds.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                    .Select(s => { int.TryParse(s, out var v); return v; })
                    .Where(v => v != 0).ToArray();
            }

            var result = await _auditRepo.QueryAsync(query);
            return Ok(result);
        }
    }
}
