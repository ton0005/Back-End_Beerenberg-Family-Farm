using System;
using FarmManagement.Core.Enums;

namespace FarmManagement.Core.Entities.Payroll
{
    /// <summary>
    /// Represents hourly pay rates based on Australian Horticulture Award
    /// Supports historical rate changes and different contract types
    /// </summary>
    public class PayRate
    {
        public int PayRateId { get; set; }

        /// <summary>
        /// Contract type this rate applies to
        /// </summary>
        public ContractTypeEnum ContractType { get; set; }

        /// <summary>
        /// Rate type: Regular, Overtime, Weekend, PublicHoliday (extensible)
        /// </summary>
        public string RateType { get; set; } = "Regular";

        /// <summary>
        /// Hourly rate amount
        /// </summary>
        public decimal HourlyRate { get; set; }

        /// <summary>
        /// Date this rate becomes effective
        /// </summary>
        public DateTime EffectiveFrom { get; set; }

        /// <summary>
        /// Date this rate expires (null if current)
        /// </summary>
        public DateTime? EffectiveTo { get; set; }

        /// <summary>
        /// Is this the currently active rate
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Description or notes about this rate (e.g., "Based on Horticulture Award 2025")
        /// </summary>
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }
}
