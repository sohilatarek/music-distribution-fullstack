using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicDistribution.Domain.Entities;

namespace MusicDistribution.Infrastructure.Persistence.Configurations;

public class DspConfiguration : IEntityTypeConfiguration<Dsp>
{
    public void Configure(EntityTypeBuilder<Dsp> builder)
    {
        builder.ToTable("Dsps");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(d => d.Name).IsUnique();
    }
}
