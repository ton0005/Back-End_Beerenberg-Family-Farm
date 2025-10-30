using System;

namespace FarmManagement.Application.DTOs
{
    public class PayCalendarDto
    {
        public int PayCalendarId { get; set; }
        public DateTime StartPeriodDate { get; set; }
        public DateTime EndPeriodDate { get; set; }
        public DateTime PayDate { get; set; }
        public string PayFrequency { get; set; } = "Fortnightly";
        public string Status { get; set; } = "Active";
        public bool IsPayrollGenerated { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class CreatePayCalendarRequest
    {
        /// <summary>
        /// Start date of the pay period (DD/MM/YYYY)
        /// </summary>
        public DateTime StartPeriodDate { get; set; }

        /// <summary>
        /// Pay date - must be after the end period date (DD/MM/YYYY)
        /// </summary>
        public DateTime PayDate { get; set; }
    }

    public class PayCalendarListResponse
    {
        public int PayCalendarId { get; set; }
        public DateTime StartPeriodDate { get; set; }
        public DateTime EndPeriodDate { get; set; }
        public DateTime PayDate { get; set; }
        public string PayFrequency { get; set; } = "Fortnightly";
        public string Status { get; set; } = "Active";
        public bool IsPayrollGenerated { get; set; }
    }
}
