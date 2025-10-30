using System;
using FarmManagement.Core.Enums;

namespace FarmManagement.Core.Entities.Payroll
{
    /// <summary>
    /// Represents individual staff payroll entry for a pay period
    /// </summary>
    public class PayrollLineItem
    {
        public int PayrollLineItemId { get; set; }

        /// <summary>
        /// Foreign key to PayrollRun
        /// </summary>
        public int PayrollRunId { get; set; }
        public PayrollRun? PayrollRun { get; set; }

        /// <summary>
        /// Staff number (FK to Staff)
        /// </summary>
        public string StaffNumber { get; set; } = string.Empty;
        public Staff? Staff { get; set; }

        /// <summary>
        /// Staff first name (snapshot for historical accuracy)
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Staff last name (snapshot for historical accuracy)
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Contract type at the time of payroll calculation
        /// </summary>
        public ContractTypeEnum ContractType { get; set; }

        /// <summary>
        /// Total regular hours worked
        /// </summary>
        public decimal RegularHours { get; set; }

        /// <summary>
        /// Overtime hours (for casual staff working >12 hours/day)
        /// </summary>
        public decimal OvertimeHours { get; set; }

        /// <summary>
        /// Regular hourly rate applied
        /// </summary>
        public decimal RegularHourlyRate { get; set; }

        /// <summary>
        /// Overtime hourly rate applied (for casual overtime)
        /// </summary>
        public decimal OvertimeHourlyRate { get; set; }

        /// <summary>
        /// Gross wages (before any deductions)
        /// </summary>
        public decimal GrossWages { get; set; }

        /// <summary>
        /// Net wages (after deductions) - Currently same as gross, extensible for future
        /// </summary>
        public decimal NetWages { get; set; }

        /// <summary>
        /// Total hours (regular + overtime)
        /// </summary>
        public decimal TotalHours { get; set; }

        /// <summary>
        /// Notes or comments for this line item
        /// </summary>
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
