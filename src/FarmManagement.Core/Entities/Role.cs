namespace FarmManagement.Core.Entities
{
    public class Role
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty; // Admin, Supervisor, Picker, Packer

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
         // Navigation
        public ICollection<StaffRole> StaffRoles { get; set; } = new List<StaffRole>();
    
    }
}