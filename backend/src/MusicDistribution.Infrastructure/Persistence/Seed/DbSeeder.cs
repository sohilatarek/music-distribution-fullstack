using Microsoft.EntityFrameworkCore;
using MusicDistribution.Domain.Entities;
using MusicDistribution.Domain.Enums;

namespace MusicDistribution.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent seeder run at startup. Uses runtime seeding (rather than migration HasData)
/// so it is easy to read, extend, and re-run against a fresh database.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Artists.AnyAsync())
        {
            return; // already seeded
        }

        var artists = new List<Artist>
        {
            new() { Name = "Luna Ray",        Email = "luna.ray@example.com",        Country = "United States" },
            new() { Name = "The Midnight Set", Email = "contact@midnightset.example", Country = "United Kingdom" },
            new() { Name = "Kenji Arata",      Email = "kenji.arata@example.com",     Country = "Japan" },
            new() { Name = "Sofia Marchetti",  Email = "sofia.marchetti@example.com", Country = "Italy" }
        };
        db.Artists.AddRange(artists);
        await db.SaveChangesAsync();

        var dsps = new List<Dsp>
        {
            new() { Name = "Spotify" },
            new() { Name = "Apple Music" },
            new() { Name = "YouTube Music" }
        };
        db.Dsps.AddRange(dsps);
        await db.SaveChangesAsync();

        var tracks = new List<Track>
        {
            new() { Title = "Neon Skyline",       ArtistId = artists[0].Id, Isrc = "USRC17600001", Genre = "Synthpop",  Status = TrackStatus.Distributed, ReleaseDate = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc) },
            new() { Title = "Afterglow",           ArtistId = artists[0].Id, Isrc = "USRC17600002", Genre = "Pop",       Status = TrackStatus.Submitted,   ReleaseDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
            new() { Title = "Wolves at the Door",  ArtistId = artists[1].Id, Isrc = "GBUM72100003", Genre = "Rock",      Status = TrackStatus.Distributed, ReleaseDate = new DateTime(2023, 11, 10, 0, 0, 0, DateTimeKind.Utc) },
            new() { Title = "Static Heart",        ArtistId = artists[1].Id, Isrc = "GBUM72100004", Genre = "Alt Rock",  Status = TrackStatus.Draft,       ReleaseDate = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc) },
            new() { Title = "Rain on Glass",       ArtistId = artists[2].Id, Isrc = "JPKR12300005", Genre = "Lo-fi",     Status = TrackStatus.Distributed, ReleaseDate = new DateTime(2024, 8, 5, 0, 0, 0, DateTimeKind.Utc) },
            new() { Title = "Tokyo Drift Home",    ArtistId = artists[2].Id, Isrc = "JPKR12300006", Genre = "Electronic",Status = TrackStatus.Submitted,   ReleaseDate = new DateTime(2024, 9, 12, 0, 0, 0, DateTimeKind.Utc) },
            new() { Title = "Vento del Sud",       ArtistId = artists[3].Id, Isrc = "ITALY2100007", Genre = "Indie",     Status = TrackStatus.Draft,       ReleaseDate = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc) },
            new() { Title = "Cielo Blu",           ArtistId = artists[3].Id, Isrc = "ITALY2100008", Genre = "Pop",       Status = TrackStatus.Distributed, ReleaseDate = new DateTime(2023, 5, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Title = "Concrete Bloom",      ArtistId = artists[1].Id, Isrc = "GBUM72100009", Genre = "Hip-Hop",   Status = TrackStatus.Submitted,   ReleaseDate = new DateTime(2024, 12, 3, 0, 0, 0, DateTimeKind.Utc) }
        };
        db.Tracks.AddRange(tracks);
        await db.SaveChangesAsync();

        var spotify = dsps.First(d => d.Name == "Spotify");
        var appleMusic = dsps.First(d => d.Name == "Apple Music");
        var youtube = dsps.First(d => d.Name == "YouTube Music");

        var distributions = new List<TrackDistribution>
        {
            new() { TrackId = tracks[0].Id, DspId = spotify.Id,     Status = DistributionStatus.Live,     SubmittedAt = new DateTime(2024, 3, 16, 0, 0, 0, DateTimeKind.Utc) },
            new() { TrackId = tracks[0].Id, DspId = appleMusic.Id,  Status = DistributionStatus.Live,     SubmittedAt = new DateTime(2024, 3, 16, 0, 0, 0, DateTimeKind.Utc) },
            new() { TrackId = tracks[0].Id, DspId = youtube.Id,     Status = DistributionStatus.Rejected, SubmittedAt = new DateTime(2024, 3, 16, 0, 0, 0, DateTimeKind.Utc) },

            new() { TrackId = tracks[1].Id, DspId = spotify.Id,     Status = DistributionStatus.Pending,  SubmittedAt = new DateTime(2024, 6, 2, 0, 0, 0, DateTimeKind.Utc) },

            new() { TrackId = tracks[2].Id, DspId = spotify.Id,     Status = DistributionStatus.Live,     SubmittedAt = new DateTime(2023, 11, 11, 0, 0, 0, DateTimeKind.Utc) },
            new() { TrackId = tracks[2].Id, DspId = youtube.Id,     Status = DistributionStatus.Live,     SubmittedAt = new DateTime(2023, 11, 11, 0, 0, 0, DateTimeKind.Utc) },

            new() { TrackId = tracks[4].Id, DspId = spotify.Id,     Status = DistributionStatus.Live,     SubmittedAt = new DateTime(2024, 8, 6, 0, 0, 0, DateTimeKind.Utc) },
            new() { TrackId = tracks[4].Id, DspId = appleMusic.Id,  Status = DistributionStatus.Pending,  SubmittedAt = new DateTime(2024, 8, 6, 0, 0, 0, DateTimeKind.Utc) },

            new() { TrackId = tracks[5].Id, DspId = appleMusic.Id,  Status = DistributionStatus.Pending,  SubmittedAt = new DateTime(2024, 9, 13, 0, 0, 0, DateTimeKind.Utc) },

            new() { TrackId = tracks[7].Id, DspId = spotify.Id,     Status = DistributionStatus.Live,     SubmittedAt = new DateTime(2023, 5, 23, 0, 0, 0, DateTimeKind.Utc) },
            new() { TrackId = tracks[7].Id, DspId = appleMusic.Id,  Status = DistributionStatus.Live,     SubmittedAt = new DateTime(2023, 5, 23, 0, 0, 0, DateTimeKind.Utc) },
            new() { TrackId = tracks[7].Id, DspId = youtube.Id,     Status = DistributionStatus.Live,     SubmittedAt = new DateTime(2023, 5, 23, 0, 0, 0, DateTimeKind.Utc) },

            new() { TrackId = tracks[8].Id, DspId = youtube.Id,     Status = DistributionStatus.Pending,  SubmittedAt = new DateTime(2024, 12, 4, 0, 0, 0, DateTimeKind.Utc) }
        };
        db.TrackDistributions.AddRange(distributions);
        await db.SaveChangesAsync();
    }
}
