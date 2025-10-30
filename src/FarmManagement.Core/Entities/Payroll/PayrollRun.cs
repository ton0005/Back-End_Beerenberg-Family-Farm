using System;
using System.Collections.Generic;

namespace FarmManagement.Core.Entities.Payroll
{
    /// <summary>
    /// Represents a payroll processing run for a specific pay period
    /// </summary>
    public class PayrollRun
    {
        public int PayrollRunId { get; set; }

        /// <summary>
        /// Foreign key to PayCalendar
        /// </summary>
        public int PayCalendarId { get; set; }
        public PayCalendar? PayCalendar { get; set; }

        /// <summary>
        /// Total labour cost for this payroll run (sum of all line items)
        /// </summary>
        public decimal TotalLabourCost { get; set; }

        /// <summary>
        /// Total work hours across all staff for this pay period
        /// </summary>
        public decimal TotalWorkHours { get; set; }

        /// <summary>
        /// Number of staff members included in this payroll run
        /// </summary>
        public int StaffCount { get; set; }

        /// <summary>
        /// Status: Draft, Pending, Approved, Paid, Cancelled
        /// </summary>
        public string Status { get; set; } = "Draft";

        /// <summary>
        /// Run number for tracking (incremental per pay calendar)
        /// </summary>
        public int RunNumber { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? PaidAt { get; set; }

        // Navigation properties
        public ICollection<PayrollLineItem> LineItems { get; set; } = new List<PayrollLineItem>();
    }
}
