using System;
using System.Collections.Generic;

namespace FarmManagement.Application.DTOs
{
    public class PayrollRunDto
    {
        public int PayrollRunId { get; set; }
        public int PayCalendarId { get; set; }
        public decimal TotalLabourCost { get; set; }
        public decimal TotalWorkHours { get; set; }
        public int StaffCount { get; set; }
        public string Status { get; set; } = "Draft";
        public int RunNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }

        // Pay calendar details
        public DateTime StartPeriodDate { get; set; }
        public DateTime EndPeriodDate { get; set; }
        public DateTime PayDate { get; set; }

        // Line items
        public List<PayrollLineItemDto> LineItems { get; set; } = new();
    }

    public class CreatePayrollRequest
    {
        /// <summary>
        /// Pay calendar ID to generate payroll for
        /// </summary>
        public int PayCalendarId { get; set; }
    }

    public class PayrollSummaryDto
    {
        public int PayrollRunId { get; set; }
        public int PayCalendarId { get; set; }
        public string PayCalendarPeriod { get; set; } = string.Empty;
        public decimal TotalLabourCost { get; set; }
        public decimal TotalWorkHours { get; set; }
        public int StaffCount { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime CreatedAt { get; set; }
    }
}
