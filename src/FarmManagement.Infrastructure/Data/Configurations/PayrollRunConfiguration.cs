using FarmManagement.Core.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
    {
        public void Configure(EntityTypeBuilder<PayrollRun> builder)
        {
            builder.HasKey(pr => pr.PayrollRunId);

            builder.Property(pr => pr.TotalLabourCost)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pr => pr.TotalWorkHours)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pr => pr.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Draft");

            builder.Property(pr => pr.CreatedBy)
                .HasMaxLength(50);

            builder.Property(pr => pr.ApprovedBy)
                .HasMaxLength(50);

            // Relationship with PayCalendar
            builder.HasOne(pr => pr.PayCalendar)
                .WithMany(pc => pc.PayrollRuns)
                .HasForeignKey(pr => pr.PayCalendarId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for queries
            builder.HasIndex(pr => pr.PayCalendarId);
            builder.HasIndex(pr => pr.Status);
            builder.HasIndex(pr => pr.CreatedAt);
        }
    }
}
