using FarmManagement.Core.Enums;
using System.Text.Json.Serialization;
using System;

namespace FarmManagement.Core.Entities
{
    public class Staff
    {
        public int StaffId { get; set; }

        public string StaffNumber { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        // Use enum directly instead of FK int
        public ContractTypeEnum ContractType { get; set; }
        public int? DepartmentId { get; set; }             // FK to Departments
    [JsonIgnore]
    public Department? Department { get; set; }

    // Weekly available working hours for this staff member (required at API level)
    public int WeeklyAvailableHour { get; set; }

        public DateTime? HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
