using System;

namespace FarmManagement.Api.ApiModels.Shifts
{
	public class ShiftCreateRequest
	{
		public string ShiftTypeName { get; set; } = string.Empty;
		public DateTime Date { get; set; }
		public IReadOnlyCollection<string> StaffNumbers { get; set; } = [];
	}
}
