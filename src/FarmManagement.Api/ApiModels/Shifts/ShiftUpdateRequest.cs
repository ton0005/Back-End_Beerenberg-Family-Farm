using System;

namespace FarmManagement.Api.ApiModels.Shifts
{
	public class ShiftUpdateRequest
	{
		// Accept template name (e.g. "Morning", "Afternoon", "FullDay", or "Custom"). If provided, name takes precedence over ShiftTypeId.
		public string? ShiftTypeName { get; set; }
		public int ShiftTypeId { get; set; }
		public DateTime Date { get; set; }
		public TimeSpan? StartTime { get; set; }
		public TimeSpan? EndTime { get; set; }
		public TimeSpan? Break { get; set; }
		public bool IsPublished { get; set; }
		public string? Note { get; set; }

		// Optional: when provided, this represents the complete desired set of staff assignments (by StaffNumber) for the shift.
		// Any existing assignments whose StaffNumber is not in this list will be removed, and any new StaffNumbers will be added.
		// Provide an empty array to clear all assignments. Omit (null) to leave assignments unchanged.
		public IEnumerable<string>? StaffNumbers { get; set; }
	}
}
