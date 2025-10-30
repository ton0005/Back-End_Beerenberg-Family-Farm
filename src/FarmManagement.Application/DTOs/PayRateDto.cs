using System;
using FarmManagement.Core.Enums;

namespace FarmManagement.Application.DTOs
{
    public class PayRateDto
    {
        public int PayRateId { get; set; }
        public string ContractType { get; set; } = string.Empty;
        public string RateType { get; set; } = "Regular";
        public decimal HourlyRate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
    }
}
