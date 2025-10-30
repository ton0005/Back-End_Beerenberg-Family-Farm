using FarmManagement.Core.Enums;
using System;

namespace FarmManagement.Api.ApiModels.Shifts
{
	public class ShiftAssignRequest
	{
		// Use StaffNumber for assignment 
		public string StaffNumber { get; set; } = string.Empty;

		// Allow assigning multiple staff in one request. If provided, items in this list will be processed.
		public IEnumerable<string>? StaffNumbers { get; set; }
		public AssignmentStatusEnum Status { get; set; } = AssignmentStatusEnum.Assigned;
		public DateTime? AssignedAt { get; set; }
	}
}
