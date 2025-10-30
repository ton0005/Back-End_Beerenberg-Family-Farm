using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class AuthUserConfiguration : IEntityTypeConfiguration<AuthUser>
    {
        public void Configure(EntityTypeBuilder<AuthUser> builder)
        {
            builder.HasKey(a => a.AuthId);

            builder.HasIndex(a => a.Username).IsUnique();

            builder.Property(a => a.Username)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.IdentityUserId)
                .HasMaxLength(450)
                .IsRequired(false);

            builder.Property(a => a.PasswordHash)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(a => a.PasswordSalt)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(a => a.IsActive)
                .IsRequired();

            builder.Property(a => a.LastLogin);
            builder.Property(a => a.CreatedAt).IsRequired();
            builder.Property(a => a.UpdatedAt);

            builder.HasOne(a => a.Staff)
                .WithMany()
                .HasForeignKey(a => a.StaffId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
