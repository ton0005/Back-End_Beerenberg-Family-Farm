using System;
using System.Collections.Generic;

namespace FarmManagement.Application.DTOs
{
    public class BreakIntervalDto
    {
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
    }

    public class StaffSessionDto
    {
        public string StaffNumber { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly Date { get; set; }
        public DateTime? ClockIn { get; set; }
        // Back-compat: first break window for clients not yet migrated to Breaks[]
        public DateTime? BreakStart { get; set; }
        public DateTime? BreakEnd { get; set; }
        public DateTime? ClockOut { get; set; }

        public List<BreakIntervalDto> Breaks { get; set; } = new();

        // Derived fields
        public int? TotalBreakMinutes { get; set; }
        public int? WorkedMinutes { get; set; }
    }
}
