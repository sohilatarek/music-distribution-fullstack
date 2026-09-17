using Microsoft.EntityFrameworkCore;
using MusicDistribution.Domain.Entities;

namespace MusicDistribution.Application.Interfaces;

/// <summary>
/// Abstraction over the persistence context so the Application layer never depends
/// directly on EF Core / Infrastructure (Clean Architecture dependency rule).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Artist> Artists { get; }
    DbSet<Track> Tracks { get; }
    DbSet<Dsp> Dsps { get; }
    DbSet<TrackDistribution> TrackDistributions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
