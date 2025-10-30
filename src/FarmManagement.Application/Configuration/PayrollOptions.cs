using System;

namespace FarmManagement.Application.Configuration
{
    public class PayrollOptions
    {
    public string? PayFrequency { get; set; }
    public int? FortnightlyDays { get; set; }
    public int? CasualOvertimeThresholdHours { get; set; }
        // Number of minutes of paid break to add per session (e.g., 10)
        public int PaidBreakMinutes { get; set; }
    }
}
