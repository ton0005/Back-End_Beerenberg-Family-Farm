using System;

namespace FarmManagement.Api.ApiModels.Shifts
{
	public class ShiftView
	{
		public int ShiftId { get; set; }
		public int ShiftTypeId { get; set; }
		public DateTime Date { get; set; }
		public TimeSpan? StartTime { get; set; }
		public TimeSpan? EndTime { get; set; }
		public string? Note { get; set; }
		public TimeSpan? Break { get; set; }
		public bool IsPublished { get; set; }
		public IEnumerable<ShiftAssignmentView>? Assignments { get; set; }
	}
}
