using System;
using FarmManagement.Core.Enums;

namespace FarmManagement.Api.ApiModels.Shifts
{
	public class ShiftAssignmentView
	{
		public int ShiftAssignmentId { get; set; }
		public int ShiftId { get; set; }
		// Hide internal StaffId, expose StaffNumber only
		public string StaffNumber { get; set; } = string.Empty;
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? RoleName { get; set; }
        // Shift details
        public string? ShiftTypeName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
		public AssignmentStatusEnum Status { get; set; }
		public DateTime AssignedAt { get; set; }
		public DateTime? CompletedAt { get; set; }
	}
}
