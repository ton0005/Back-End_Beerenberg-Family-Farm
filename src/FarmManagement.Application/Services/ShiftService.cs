using FarmManagement.Application.DTOs;
using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FarmManagement.Core.Enums;

namespace FarmManagement.Application.Services
{
    public class ShiftService : IShiftService
    {
        private readonly IShiftTypeRepository _shiftTypeRepo;
        private readonly IShiftRepository _shiftRepo;
        private readonly IShiftAssignmentRepository _assignmentRepo;
        private readonly IStaffRepository _staffRepo;
        private readonly IStaffRoleRepository _staffRoleRepo;

        public ShiftService(IShiftTypeRepository shiftTypeRepo, IShiftRepository shiftRepo, IShiftAssignmentRepository assignmentRepo, IStaffRepository staffRepo, IStaffRoleRepository staffRoleRepo)
        {
            _shiftTypeRepo = shiftTypeRepo;
            _shiftRepo = shiftRepo;
            _assignmentRepo = assignmentRepo;
            _staffRepo = staffRepo;
            _staffRoleRepo = staffRoleRepo;
        }

        public async Task<IEnumerable<ShiftTypeDto>> GetShiftTypesAsync()
        {
            var types = await _shiftTypeRepo.GetAllAsync();
            return types.Select(t => new ShiftTypeDto
            {
                ShiftTypeId = t.ShiftTypeId,
                Name = t.Name,
                DefaultStartTime = t.DefaultStartTime,
                DefaultEndTime = t.DefaultEndTime,
                Description = t.Description
            });
        }

        public async Task<(ShiftDto?, string)> CreateShiftAsync(ShiftDto dto)
        {
            var shiftType = await _shiftTypeRepo.GetByTypeNameAsync(dto.ShiftTypeName);
            if (shiftType == null)
            {
                return (null, "Shift type is not found");
            }

            var shift = new Shift
            {
                ShiftTypeId = shiftType.ShiftTypeId,
                Date = dto.Date.Date,
                StartTime = shiftType.DefaultStartTime,
                EndTime = shiftType.DefaultEndTime,
                Note = string.Empty,
                Break = null,
                IsPublished = true
            };

            _shiftRepo.Add(shift);

            var staffNumbers = dto.Assignments.Select(s => s.StaffNumber).ToArray();
            var staffs = await _staffRepo.GetStaffsAsync(staffNumbers);
            if (staffs.Count == 0)
            {
                return (null, "Staff numbers are not found");
            }

            foreach (var staff in staffs)
            {
                if (await _assignmentRepo.IsAssignmentOverlappedAsync(staff.StaffId, shift.Date,
                        shiftType.DefaultStartTime, shiftType.DefaultEndTime))
                {
                    return (null, $"{staff.FirstName.Trim()} {staff.LastName.Trim()} has assignment overlap");
                }

                var assignment = new ShiftAssignment
                {
                    StaffId = staff.StaffId,
                    Shift = shift,
                    Status = AssignmentStatusEnum.Assigned,
                    AssignedAt = DateTime.Now,
                };
                _assignmentRepo.Add(assignment);
            }
            
            await _shiftRepo.SaveChangesAsync();

            var shiftDto = new ShiftDto
            {
                ShiftId = shift.ShiftId,
                ShiftTypeId = shift.ShiftTypeId,
                Date = shift.Date,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                Break = shift.Break,
                Note = shift.Note,
                IsPublished = shift.IsPublished
            };
            return (shiftDto, string.Empty);
        }

        public async Task<bool> StaffExistsAsync(string staffNumber)
        {
            if (string.IsNullOrWhiteSpace(staffNumber)) return false;
            var staff = await _staffRepo.GetByStaffNumberAsync(staffNumber.Trim());
            return staff != null;
        }

        public async Task<ShiftDto?> GetShiftByIdAsync(int id)
        {
            var shift = await _shiftRepo.GetByIdAsync(id);
            if (shift == null) return null;

            return new ShiftDto
            {
                ShiftId = shift.ShiftId,
                ShiftTypeId = shift.ShiftTypeId,
                Date = shift.Date,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                Break = shift.Break,
                Note = shift.Note,
                IsPublished = shift.IsPublished
            };
        }

        public async Task<ShiftDto?> UpdateShiftAsync(int id, ShiftDto dto)
        {
            var existing = await _shiftRepo.GetByIdAsync(id);
            if (existing == null) return null;
            // Resolve ShiftTypeName if provided
            ShiftType? resolvedType = null;
            if (!string.IsNullOrWhiteSpace(dto.ShiftTypeName))
            {
                var all = await _shiftTypeRepo.GetAllAsync();
                resolvedType = all.FirstOrDefault(t => string.Equals(t.Name, dto.ShiftTypeName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (resolvedType != null) existing.ShiftTypeId = resolvedType.ShiftTypeId;
                // If resolved template is Custom, ensure start/end supplied
                if (resolvedType != null && string.Equals(resolvedType.Name, "Custom", StringComparison.OrdinalIgnoreCase))
                {
                    if (!dto.StartTime.HasValue || !dto.EndTime.HasValue)
                        throw new InvalidOperationException("StartTime and EndTime are required for 'Custom' shift type.");
                }
                else if (resolvedType != null)
                {
                    // For named templates (Morning/Afternoon/FullDay), if start/end not provided, apply defaults
                    if (!dto.StartTime.HasValue && resolvedType.DefaultStartTime != TimeSpan.Zero)
                        dto.StartTime = resolvedType.DefaultStartTime;
                    if (!dto.EndTime.HasValue && resolvedType.DefaultEndTime != TimeSpan.Zero)
                        dto.EndTime = resolvedType.DefaultEndTime;
                }
            }
            else if (dto.ShiftTypeId != 0)
            {
                // Try loading by id to possibly apply defaults or enforce Custom
                resolvedType = await _shiftTypeRepo.GetByIdAsync(dto.ShiftTypeId);
                if (resolvedType != null && string.Equals(resolvedType.Name, "Custom", StringComparison.OrdinalIgnoreCase))
                {
                    if (!dto.StartTime.HasValue || !dto.EndTime.HasValue)
                        throw new InvalidOperationException("StartTime and EndTime are required for 'Custom' shift type.");
                }
                else if (resolvedType != null)
                {
                    if (!dto.StartTime.HasValue && resolvedType.DefaultStartTime != TimeSpan.Zero)
                        dto.StartTime = resolvedType.DefaultStartTime;
                    if (!dto.EndTime.HasValue && resolvedType.DefaultEndTime != TimeSpan.Zero)
                        dto.EndTime = resolvedType.DefaultEndTime;
                }
            }
            else
            {
                existing.ShiftTypeId = dto.ShiftTypeId;
            }
            existing.Date = dto.Date.Date;
            existing.StartTime = dto.StartTime;
            existing.EndTime = dto.EndTime;
            existing.Note = dto.Note;
            existing.Break = dto.Break;
            existing.IsPublished = dto.IsPublished;

            var updated = await _shiftRepo.UpdateAsync(existing);
            if (updated == null) return null;

            return new ShiftDto
            {
                ShiftId = updated.ShiftId,
                ShiftTypeId = updated.ShiftTypeId,
                Date = updated.Date,
                StartTime = updated.StartTime,
                EndTime = updated.EndTime,
                Break = updated.Break,
                Note = updated.Note,
                IsPublished = updated.IsPublished
            };
        }

        public async Task<bool> DeleteShiftAsync(int id)
        {
            return await _shiftRepo.DeleteAsync(id);
        }

        public async Task<bool> PublishShiftAsync(int id)
        {
            var existing = await _shiftRepo.GetByIdAsync(id);
            if (existing == null) return false;
            existing.IsPublished = true;
            var updated = await _shiftRepo.UpdateAsync(existing);
            return updated != null;
        }

        public async Task<bool> UnpublishShiftAsync(int id)
        {
            var existing = await _shiftRepo.GetByIdAsync(id);
            if (existing == null) return false;
            existing.IsPublished = false;
            var updated = await _shiftRepo.UpdateAsync(existing);
            return updated != null;
        }

        public async Task<IEnumerable<ShiftAssignmentDto>> GetAssignedShiftsForStaffAsync(int staffId)
        {
            // Only include assignments where the underlying Shift is published
            var assignments = (await _assignmentRepo.GetByStaffIdAsync(staffId)).Where(a => a.Shift != null && a.Shift.IsPublished);
            return assignments.Select(a => new ShiftAssignmentDto
            {
                ShiftAssignmentId = a.ShiftAssignmentId,
                ShiftId = a.ShiftId,
                StaffId = a.StaffId,
                Status = a.Status,
                AssignedAt = a.AssignedAt,
                CompletedAt = a.CompletedAt
            });
        }

        public async Task<IEnumerable<ShiftAssignmentDto>> GetAssignmentsByShiftIdAsync(int shiftId)
        {
            var assignments = await _assignmentRepo.GetByShiftIdAsync(shiftId);
            if (assignments == null) return Enumerable.Empty<ShiftAssignmentDto>();
            var list = new List<ShiftAssignmentDto>();
            foreach (var a in assignments)
            {
                string? roleName = null;
                try
                {
                    var rolesForStaff = await _staffRoleRepo.GetRolesByStaffIdAsync(a.StaffId);
                    var current = rolesForStaff?.Where(r => r.IsCurrent).OrderByDescending(r => r.EffectiveFrom).FirstOrDefault()
                                  ?? rolesForStaff?.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();
                    if (current != null) roleName = current.Role?.RoleName;
                }
                catch { }

                list.Add(new ShiftAssignmentDto
                {
                    ShiftAssignmentId = a.ShiftAssignmentId,
                    ShiftId = a.ShiftId,
                    StaffId = a.StaffId,
                    StaffNumber = a.Staff?.StaffNumber,
                    FirstName = a.Staff?.FirstName,
                    LastName = a.Staff?.LastName,
                    RoleName = roleName,
                    Status = a.Status,
                    AssignedAt = a.AssignedAt,
                    CompletedAt = a.CompletedAt
                });
            }

            return list.AsEnumerable();
        }

    public async Task<PagedResult<ShiftDto>> GetAllShiftsAsync(int page = 1, int pageSize = 20, DateTime? startDate = null, DateTime? endDate = null, int? shiftTypeId = null, string? search = null, string? sortBy = null, bool sortDesc = false, bool onlyPublished = false, CancellationToken ct = default)
        {
            var result = await _shiftRepo.GetAllAsync(page, pageSize, startDate, endDate, shiftTypeId, search, sortBy, sortDesc, onlyPublished, ct);
            return new PagedResult<ShiftDto>
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                Items = result.Items.Select(s => new ShiftDto
                {
                    ShiftId = s.ShiftId,
                    ShiftTypeId = s.ShiftTypeId,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Break = s.Break,
                    Note = s.Note,
                    IsPublished = s.IsPublished
                })
            };
        }

    public async Task<IEnumerable<ShiftAssignmentDto>> GetAssignmentsByStaffNumberAsync(string staffNumber, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(staffNumber)) return Enumerable.Empty<ShiftAssignmentDto>();
            var staff = await _staffRepo.GetByStaffNumberAsync(staffNumber.Trim(), ct);
            if (staff == null) return Enumerable.Empty<ShiftAssignmentDto>();
            var assignments = await _assignmentRepo.GetByStaffIdAsync(staff.StaffId);
            var roleName = (string?)null;
            try
            {
                var rolesForStaff = await _staffRoleRepo.GetRolesByStaffIdAsync(staff.StaffId);
                var current = rolesForStaff?.Where(r => r.IsCurrent).OrderByDescending(r => r.EffectiveFrom).FirstOrDefault()
                              ?? rolesForStaff?.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();
                if (current != null) roleName = current.Role?.RoleName;
            }
            catch { }

            return assignments.Select(a => new ShiftAssignmentDto
            {
                ShiftAssignmentId = a.ShiftAssignmentId,
                ShiftId = a.ShiftId,
                StaffId = a.StaffId,
                StaffNumber = staff.StaffNumber,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                RoleName = roleName,
                Status = a.Status,
                AssignedAt = a.AssignedAt,
                CompletedAt = a.CompletedAt
            });
        }

        public async Task<FarmManagement.Application.DTOs.PagedResult<FarmManagement.Application.DTOs.PublicShiftDto>> GetPublicShiftsForStaffAsync(int staffId, int page = 1, int pageSize = 20, DateTime? startDate = null, DateTime? endDate = null, int? shiftTypeId = null, string? search = null, string? sortBy = null, bool sortDesc = false, CancellationToken ct = default)
        {
            var paged = await _assignmentRepo.GetByStaffIdPagedAsync(staffId, page, pageSize, startDate, endDate, shiftTypeId, search, sortBy, sortDesc, ct);

            var items = paged.Items.Select(a => new FarmManagement.Application.DTOs.PublicShiftDto
            {
                ShiftId = a.ShiftId,
                ShiftType = a.Shift?.ShiftType?.Name,
                Date = a.Shift?.Date ?? DateTime.MinValue,
                StartTime = a.Shift?.StartTime,
                EndTime = a.Shift?.EndTime,
                Break = a.Shift?.Break,
                Status = a.Status.ToString(),
                AssignedAt = a.AssignedAt,
                CompletedAt = a.CompletedAt
            });

            return new FarmManagement.Application.DTOs.PagedResult<FarmManagement.Application.DTOs.PublicShiftDto>
            {
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                Items = items
            };
        }

        public async Task<bool> DeleteAssignmentAsync(int assignmentId)
        {
            return await _assignmentRepo.DeleteAsync(assignmentId);
        }
    }
}
