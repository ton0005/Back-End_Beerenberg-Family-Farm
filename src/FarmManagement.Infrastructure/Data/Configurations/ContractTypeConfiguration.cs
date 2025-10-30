using FarmManagement.Core.Entities;
using FarmManagement.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class ContractTypeConfiguration : IEntityTypeConfiguration<ContractType>
    {
        public void Configure(EntityTypeBuilder<ContractType> builder)
        {
            builder.HasKey(ct => ct.ContractTypeId);
            builder.Property(ct => ct.TypeName)
                   .IsRequired()
                   .HasMaxLength(50);

            // Seed enum values into DB
            builder.HasData(
                new ContractType { ContractTypeId = (int)ContractTypeEnum.Casual, TypeName = "Casual" },
                new ContractType { ContractTypeId = (int)ContractTypeEnum.PartTime, TypeName = "Part-Time" },
                new ContractType { ContractTypeId = (int)ContractTypeEnum.FullTime, TypeName = "Full-Time" }
            );
        }
    }
}
