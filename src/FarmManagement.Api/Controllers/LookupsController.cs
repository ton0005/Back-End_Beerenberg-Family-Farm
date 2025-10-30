using FarmManagement.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Api.Controllers
{
    [ApiController]
    [Route("api/lookups")]
    public class LookupsController : ControllerBase
    {
        private readonly IExceptionTypeRepository _exceptionTypeRepo;
        public LookupsController(IExceptionTypeRepository exceptionTypeRepo)
            => _exceptionTypeRepo = exceptionTypeRepo;

        // Admin only: exception types (only admins can manage / view full list)
        [HttpGet("exception-types")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetExceptionTypes(CancellationToken ct)
        {
            var list = await _exceptionTypeRepo.GetAllAsync(ct);
            var dto = list.Select(e => new { e.TypeId, e.TypeName, e.Description });
            return Ok(dto);
        }
    }
}