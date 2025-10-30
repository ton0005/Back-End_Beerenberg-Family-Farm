using FarmManagement.Core.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class PayRateConfiguration : IEntityTypeConfiguration<PayRate>
    {
        public void Configure(EntityTypeBuilder<PayRate> builder)
        {
            builder.HasKey(pr => pr.PayRateId);

            builder.Property(pr => pr.ContractType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(pr => pr.RateType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Regular");

            builder.Property(pr => pr.HourlyRate)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(pr => pr.EffectiveFrom)
                .IsRequired();

            builder.Property(pr => pr.IsActive)
                .HasDefaultValue(true);

            builder.Property(pr => pr.Description)
                .HasMaxLength(500);

            builder.Property(pr => pr.CreatedBy)
                .HasMaxLength(50);

            // Indexes for efficient rate lookups
            builder.HasIndex(pr => new { pr.ContractType, pr.RateType, pr.IsActive });
            builder.HasIndex(pr => pr.EffectiveFrom);
        }
    }
}
