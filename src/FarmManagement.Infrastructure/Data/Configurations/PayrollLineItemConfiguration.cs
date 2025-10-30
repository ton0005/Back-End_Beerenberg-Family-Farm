using FarmManagement.Core.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class PayrollLineItemConfiguration : IEntityTypeConfiguration<PayrollLineItem>
    {
        public void Configure(EntityTypeBuilder<PayrollLineItem> builder)
        {
            builder.HasKey(pli => pli.PayrollLineItemId);

            builder.Property(pli => pli.StaffNumber)
                .IsRequired()
                .HasMaxLength(7);

            builder.Property(pli => pli.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(pli => pli.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(pli => pli.ContractType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(pli => pli.RegularHours)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pli => pli.OvertimeHours)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pli => pli.TotalHours)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pli => pli.RegularHourlyRate)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pli => pli.OvertimeHourlyRate)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pli => pli.GrossWages)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pli => pli.NetWages)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pli => pli.Notes)
                .HasMaxLength(500);

            builder.Property(pli => pli.UpdatedBy)
                .HasMaxLength(50);

            // Relationship with PayrollRun
            builder.HasOne(pli => pli.PayrollRun)
                .WithMany(pr => pr.LineItems)
                .HasForeignKey(pli => pli.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Staff (informational, not enforced FK)
            builder.HasOne(pli => pli.Staff)
                .WithMany()
                .HasForeignKey(pli => pli.StaffNumber)
                .HasPrincipalKey(s => s.StaffNumber)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(pli => pli.PayrollRunId);
            builder.HasIndex(pli => pli.StaffNumber);
        }
    }
}
