using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicDistribution.Domain.Entities;

namespace MusicDistribution.Infrastructure.Persistence.Configurations;

public class TrackDistributionConfiguration : IEntityTypeConfiguration<TrackDistribution>
{
    public void Configure(EntityTypeBuilder<TrackDistribution> builder)
    {
        builder.ToTable("TrackDistributions");
        builder.HasKey(td => td.Id);
        builder.Property(td => td.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(td => td.Track)
            .WithMany(t => t.Distributions)
            .HasForeignKey(td => td.TrackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(td => td.Dsp)
            .WithMany(d => d.Distributions)
            .HasForeignKey(td => td.DspId)
            .OnDelete(DeleteBehavior.Restrict);

        // A track can only be submitted once to a given DSP.
        builder.HasIndex(td => new { td.TrackId, td.DspId }).IsUnique();
    }
}
