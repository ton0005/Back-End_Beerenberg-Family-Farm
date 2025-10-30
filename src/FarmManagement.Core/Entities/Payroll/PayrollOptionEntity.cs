using System;

namespace FarmManagement.Core.Entities.Payroll
{
    public class PayrollOptionEntity
    {
        public int Id { get; set; }
        public string? PayFrequency { get; set; }
        public int? FortnightlyDays { get; set; }
        public int? CasualOvertimeThresholdHours { get; set; }
        public int? PaidBreakMinutes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
