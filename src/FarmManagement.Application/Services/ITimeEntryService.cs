using FarmManagement.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Application.Services
{
    public interface ITimeEntryService
    {
        Task<TimeEntryDto> ClockAsync(TimeEntryDto dto, CancellationToken ct = default);
        Task<IEnumerable<TimeEntryDto>> GetByStaffNumberAsync(string staffNumber, CancellationToken ct = default);
        Task<IEnumerable<TimeEntryDto>> GetTodayEntriesAsync(string staffNumber, CancellationToken ct = default);
        Task<TimeEntryDto> ManualEditAsync(int entryId, TimeEntryDto dto, CancellationToken ct = default);
        Task<StaffSessionDto> ManualEditSessionAsync(string staffNumber, DateOnly date, ManualSessionEditRequest request, string performedBy, CancellationToken ct = default);
        Task<ExceptionDto> CreateExceptionAsync(ExceptionDto dto, CancellationToken ct = default);
    Task<IEnumerable<ExceptionDto>> GetExceptionsAsync(string staffNumber, DateOnly date, CancellationToken ct = default);
    Task<ExceptionDto> ResolveExceptionAsync(int exceptionId, string resolvedBy, string resolutionNotes, CancellationToken ct = default);
        Task<FarmManagement.Application.DTOs.PagedResult<TimeEntryDto>> QueryAsync(string? staffNumber = null, int? entryTypeId = null, DateTime? start = null, DateTime? end = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<IEnumerable<StaffSessionDto>> GetSessionsAsync(string staffNumber, DateOnly? date = null, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken ct = default);
        Task<PagedResult<StaffSessionDto>> GetAllStaffSessionsAsync(DateOnly? date = null, DateOnly? startDate = null, DateOnly? endDate = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    }
}
