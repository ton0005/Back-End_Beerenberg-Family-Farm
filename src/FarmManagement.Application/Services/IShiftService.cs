using FarmManagement.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FarmManagement.Application.Services
{
    public interface IShiftService
    {
        Task<IEnumerable<ShiftTypeDto>> GetShiftTypesAsync();
        Task<(ShiftDto?, string)> CreateShiftAsync(ShiftDto dto);
        Task<bool> StaffExistsAsync(string staffNumber);
        Task<ShiftDto?> GetShiftByIdAsync(int id);
        Task<IEnumerable<ShiftAssignmentDto>> GetAssignmentsByShiftIdAsync(int shiftId);
        Task<ShiftDto?> UpdateShiftAsync(int id, ShiftDto dto);
        Task<bool> DeleteShiftAsync(int id);
        Task<IEnumerable<ShiftAssignmentDto>> GetAssignedShiftsForStaffAsync(int staffId);
        Task<PagedResult<ShiftDto>> GetAllShiftsAsync(int page = 1, int pageSize = 20, DateTime? startDate = null, DateTime? endDate = null, int? shiftTypeId = null, string? search = null, string? sortBy = null, bool sortDesc = false, bool onlyPublished = false, CancellationToken ct = default);
        Task<IEnumerable<ShiftAssignmentDto>> GetAssignmentsByStaffNumberAsync(string staffNumber, CancellationToken ct = default);
        Task<bool> PublishShiftAsync(int id);
        Task<bool> UnpublishShiftAsync(int id);
        Task<FarmManagement.Application.DTOs.PagedResult<FarmManagement.Application.DTOs.PublicShiftDto>> GetPublicShiftsForStaffAsync(int staffId, int page = 1, int pageSize = 20, DateTime? startDate = null, DateTime? endDate = null, int? shiftTypeId = null, string? search = null, string? sortBy = null, bool sortDesc = false, CancellationToken ct = default);
        // Delete a single assignment by its id
        Task<bool> DeleteAssignmentAsync(int assignmentId);
    }
}
