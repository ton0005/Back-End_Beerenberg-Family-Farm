using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmManagement.Core.Entities.Identity;

namespace FarmManagement.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Configure one-to-one relationship with Staff
        builder.HasOne(au => au.Staff)
            .WithOne()
            .HasForeignKey<ApplicationUser>(au => au.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // Make StaffId required
        builder.Property(au => au.StaffId)
            .IsRequired();
    }
}
