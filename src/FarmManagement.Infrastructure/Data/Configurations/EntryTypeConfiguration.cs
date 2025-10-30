using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class EntryTypeConfiguration : IEntityTypeConfiguration<EntryType>
    {
        public void Configure(EntityTypeBuilder<EntryType> builder)
        {
            builder.HasKey(e => e.EntryTypeId);
            builder.Property(e => e.TypeName).IsRequired().HasMaxLength(50);
            builder.HasIndex(e => e.TypeName).IsUnique();
        }
    }
}
