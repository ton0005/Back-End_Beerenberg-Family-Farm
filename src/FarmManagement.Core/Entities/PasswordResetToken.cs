using System;

namespace FarmManagement.Core.Entities
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        // Store StaffNumber instead of StaffId
        public string StaffNumber { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }

        // Optional navigation property if needed
        public Staff? Staff { get; set; }

        // Convenience properties
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsValid => !IsUsed && !IsExpired;
    }
}
