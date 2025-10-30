using FarmManagement.Core.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class PayCalendarConfiguration : IEntityTypeConfiguration<PayCalendar>
    {
        public void Configure(EntityTypeBuilder<PayCalendar> builder)
        {
            builder.HasKey(pc => pc.PayCalendarId);

            builder.Property(pc => pc.StartPeriodDate)
                .IsRequired();

            builder.Property(pc => pc.EndPeriodDate)
                .IsRequired();

            builder.Property(pc => pc.PayDate)
                .IsRequired();

            builder.Property(pc => pc.PayFrequency)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Fortnightly");

            builder.Property(pc => pc.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            builder.Property(pc => pc.IsPayrollGenerated)
                .HasDefaultValue(false);

            builder.Property(pc => pc.CreatedBy)
                .HasMaxLength(50);

            builder.Property(pc => pc.UpdatedBy)
                .HasMaxLength(50);

            // Index for faster date range queries
            builder.HasIndex(pc => new { pc.StartPeriodDate, pc.EndPeriodDate });
        }
    }
}
