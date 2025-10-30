using System;
using FarmManagement.Core.Enums;

namespace FarmManagement.Application.DTOs
{
    public class PayrollLineItemDto
    {
        public int PayrollLineItemId { get; set; }
        public int PayrollRunId { get; set; }
        public string StaffNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public decimal RegularHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal TotalHours { get; set; }
        public decimal RegularHourlyRate { get; set; }
        public decimal OvertimeHourlyRate { get; set; }
        public decimal GrossWages { get; set; }
        public decimal NetWages { get; set; }
        public string? Notes { get; set; }
    }
}
