using FarmManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Data.Configurations
{
    public class TimeStationConfiguration : IEntityTypeConfiguration<TimeStation>
    {
        public void Configure(EntityTypeBuilder<TimeStation> builder)
        {
            builder.HasKey(t => t.StationId);
            builder.Property(t => t.StationName).IsRequired().HasMaxLength(100);
            builder.HasIndex(t => t.StationName).IsUnique();
            builder.Property(t => t.IpAddress).HasMaxLength(50);
            builder.Property(t => t.Location).HasMaxLength(200);
        }
    }
}
