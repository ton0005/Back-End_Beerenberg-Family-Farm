using System;
using System.Collections.Generic;

namespace FarmManagement.Core.Entities.Payroll
{
    /// <summary>
    /// Represents a pay period calendar for fortnightly payroll processing
    /// </summary>
    public class PayCalendar
    {
        public int PayCalendarId { get; set; }

        /// <summary>
        /// Start date of the pay period (inclusive)
        /// </summary>
        public DateTime StartPeriodDate { get; set; }

        /// <summary>
        /// End date of the pay period (inclusive) - Auto-generated as StartDate + 13 days
        /// </summary>
        public DateTime EndPeriodDate { get; set; }

        /// <summary>
        /// Date when payment will be processed - must be after EndPeriodDate
        /// </summary>
        public DateTime PayDate { get; set; }

        /// <summary>
        /// Pay frequency - Fixed to Fortnightly
        /// </summary>
        public string PayFrequency { get; set; } = "Fortnightly";

        /// <summary>
        /// Status: Active, Completed, Cancelled
        /// </summary>
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Indicates if payroll has been generated for this period
        /// </summary>
        public bool IsPayrollGenerated { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public ICollection<PayrollRun> PayrollRuns { get; set; } = new List<PayrollRun>();
    }
}
