using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.HasKey(p => p.Id);

            // Unique indexes
            builder.HasIndex(p => p.Token).IsUnique();
            builder.HasIndex(p => p.StaffNumber); // StaffNumber index for lookups

            builder.Property(p => p.StaffNumber)
                .IsRequired()
                .HasMaxLength(7);

            builder.Property(p => p.Token)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(p => p.ExpiresAt)
                .IsRequired();

            builder.Property(p => p.IsUsed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UsedAt);

            // Optional navigation to Staff
            builder.HasOne(p => p.Staff)
                   .WithMany()
                   .HasPrincipalKey(s => s.StaffNumber)
                   .HasForeignKey(p => p.StaffNumber)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
