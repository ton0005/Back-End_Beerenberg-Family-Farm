using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class StaffRoleConfiguration : IEntityTypeConfiguration<StaffRole>
    {
        public void Configure(EntityTypeBuilder<StaffRole> builder)
        {
            builder.ToTable("StaffRoles");

            builder.HasKey(sr => sr.StaffRoleId);

            builder.Property(sr => sr.IsCurrent)
                   .HasDefaultValue(true);

            builder.Property(sr => sr.EffectiveFrom)
                   .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(sr => sr.Staff)
                   .WithMany() // a Staff can have many StaffRoles
                   .HasForeignKey(sr => sr.StaffId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sr => sr.Role)
                   .WithMany(r => r.StaffRoles)
                   .HasForeignKey(sr => sr.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
