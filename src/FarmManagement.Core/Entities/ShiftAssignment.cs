using FarmManagement.Core.Enums;
using System;

namespace FarmManagement.Core.Entities
{
    public class ShiftAssignment
    {
        public int ShiftAssignmentId { get; set; }

        public int ShiftId { get; set; }
        public Shift? Shift { get; set; }

        public int StaffId { get; set; }
        public Staff? Staff { get; set; }

        public AssignmentStatusEnum Status { get; set; } = AssignmentStatusEnum.Assigned;

        // Timestamps
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
