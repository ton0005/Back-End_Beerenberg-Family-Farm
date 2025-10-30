using FarmManagement.Core.Enums;
using System;

namespace FarmManagement.Application.DTOs
{
    public class ShiftAssignmentDto
    {
        public int ShiftAssignmentId { get; set; }
        public int ShiftId { get; set; }
        public int StaffId { get; set; }
        // Optional: allow assignment using StaffNumber instead of StaffId (Admin convenience)
        public string StaffNumber { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? RoleName { get; set; }
        public AssignmentStatusEnum Status { get; set; } = AssignmentStatusEnum.Assigned;
        public DateTime AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
