using System;
using System.Collections.Generic;

namespace FarmManagement.Application.DTOs
{
    public class BreakEditDto
    {
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
    }

    public class ManualSessionEditRequest
    {
        public DateTime? ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public List<BreakEditDto> Breaks { get; set; } = new List<BreakEditDto>();

        public int? StationId { get; set; }
        public int? ShiftAssignmentId { get; set; }
        public bool IsManual { get; set; } = true;
        public bool? BypassShiftValidation { get; set; }
        public string? BypassReason { get; set; }

        public string? ModifiedBy { get; set; }
        public string ModifiedReason { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? Status { get; set; }
    }
}
