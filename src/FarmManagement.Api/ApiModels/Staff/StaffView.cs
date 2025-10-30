namespace FarmManagement.Api.ApiModels.Staff
{
    public class StaffView
    {
        public int StaffId { get; set; }
        public string StaffNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StaffRole { get; set; }
        // True if the staff is active (no termination date). Controllers will set this based on TerminationDate/IsActive.
        public bool IsActive { get; set; }
        public string? DepartmentName { get; set; }
        public string[]? AccessRole { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string ContractType { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public int WeeklyAvailableHour { get; set; }
        // Audit timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
