using System;

namespace FarmManagement.Core.Entities
{
    public class StaffRole
    {
        public int StaffRoleId { get; set; }

        // 🔗 FK to Staff
        public int StaffId { get; set; }
        public Staff Staff { get; set; } = null!;

        // 🔗 FK to Role
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public bool IsCurrent { get; set; } = true;
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
        public DateTime? EffectiveTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
    }
}
