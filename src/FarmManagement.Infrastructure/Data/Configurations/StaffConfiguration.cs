using FarmManagement.Core.Entities;
using FarmManagement.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
       public class StaffConfiguration : IEntityTypeConfiguration<Staff>
       {
              public void Configure(EntityTypeBuilder<Staff> builder)
              {
                     builder.HasKey(s => s.StaffId);

                     builder.Property(s => s.StaffNumber)
                            .IsRequired()
                            .HasMaxLength(7)
                            .IsFixedLength(true);

                     builder.Property(s => s.FirstName)
                            .IsRequired()
                            .HasMaxLength(100);

                     builder.Property(s => s.LastName)
                            .IsRequired()
                            .HasMaxLength(100);
                     builder.Property(s => s.Email)
                                 .HasMaxLength(100);

                     builder.Property(s => s.Phone)
                            .HasMaxLength(10)
                            .IsFixedLength(true);

                     builder.Property(s => s.Address)
                            .HasMaxLength(100);

                     // Enum stored as string in DB
                     builder.Property(s => s.ContractType)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();
              }
       }
}
