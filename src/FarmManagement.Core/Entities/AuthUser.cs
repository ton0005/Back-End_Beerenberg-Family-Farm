using System;

namespace FarmManagement.Core.Entities
{
    public class AuthUser
    {
        // EF default constructor
        private AuthUser() { }

        // Constructor
        public AuthUser(int staffId, string email, string passwordHash, string passwordSalt, DateTime createdAtUtc)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                throw new ArgumentException("A valid email is required", nameof(email));

            StaffId = staffId;
            Username = email; // enforce email as username
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            PasswordSalt = passwordSalt ?? throw new ArgumentNullException(nameof(passwordSalt));
            IsActive = true;
            CreatedAt = createdAtUtc;
            FailedLoginAttempts = 0;
            IsLocked = false;
        }

        public int AuthId { get; private set; } // PK
        public int StaffId { get; private set; } // FK to Staff
        // new: optional link to AspNet Identity user Id
        public string? IdentityUserId { get; private set; }
        public string Username { get; private set; } = string.Empty; // UK
        public string PasswordHash { get; private set; } = string.Empty;
        public string PasswordSalt { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;
        public DateTime? LastLogin { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        // Account lockout properties
        public int FailedLoginAttempts { get; private set; } = 0;
        public bool IsLocked { get; private set; } = false;
        public DateTime? LockedAt { get; private set; }
        public DateTime? LockoutExpiresAt { get; private set; }

        // Navigation property
        public Staff Staff { get; private set; } = null!;

        // Methods for account lockout management
        public void IncrementFailedAttempts()
        {
            FailedLoginAttempts++;
            UpdatedAt = DateTime.UtcNow;

            if (FailedLoginAttempts >= 5)
            {
                LockAccount();
            }
        }

        public void ResetFailedAttempts()
        {
            FailedLoginAttempts = 0;
            UpdatedAt = DateTime.UtcNow;
        }

        public void LockAccount(int lockoutMinutes = 30)
        {
            IsLocked = true;
            LockedAt = DateTime.UtcNow;
            LockoutExpiresAt = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            UpdatedAt = DateTime.UtcNow;
        }

        public void UnlockAccount()
        {
            IsLocked = false;
            LockedAt = null;
            LockoutExpiresAt = null;
            FailedLoginAttempts = 0;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsAccountLocked()
        {
            if (!IsLocked) return false;

            // Check if lockout has expired
            if (LockoutExpiresAt.HasValue && DateTime.UtcNow > LockoutExpiresAt.Value)
            {
                UnlockAccount();
                return false;
            }

            return true;
        }

        public void UpdateLastLogin()
        {
            LastLogin = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdatePassword(string newPasswordHash, string newPasswordSalt)
        {
            PasswordHash = newPasswordHash;
            PasswordSalt = newPasswordSalt;
            UpdatedAt = DateTime.UtcNow;
        }
        public void LinkIdentityUser(string identityUserId)
        {
            IdentityUserId = identityUserId;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}