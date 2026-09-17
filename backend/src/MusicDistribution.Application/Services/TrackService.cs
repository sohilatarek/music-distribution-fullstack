using Microsoft.EntityFrameworkCore;
using MusicDistribution.Application.Common.Exceptions;
using MusicDistribution.Application.DTOs;
using MusicDistribution.Application.Interfaces;
using MusicDistribution.Domain.Entities;
using MusicDistribution.Domain.Enums;

namespace MusicDistribution.Application.Services;

public class TrackService : ITrackService
{
    private readonly IApplicationDbContext _db;

    /// <summary>
    /// The only transitions PATCH /api/tracks/{id}/status is allowed to make. The lifecycle is
    /// strictly forward and non-skipping: Draft -> Submitted -> Distributed. Distributed is
    /// terminal via this endpoint (an empty array of allowed next states) - there's no business
    /// case described in the brief for un-distributing a track by editing a dropdown, and allowing
    /// it would let this endpoint silently contradict whatever actually happened at the DSPs.
    /// (Draft -> Submitted also happens automatically inside DistributeAsync when a track's first
    /// submission goes out; that internal transition follows this same map, it just isn't reached
    /// through this method.)
    /// </summary>
    private static readonly Dictionary<TrackStatus, TrackStatus[]> AllowedStatusTransitions = new()
    {
        [TrackStatus.Draft] = new[] { TrackStatus.Submitted },
        [TrackStatus.Submitted] = new[] { TrackStatus.Distributed },
        [TrackStatus.Distributed] = Array.Empty<TrackStatus>()
    };

    public TrackService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TrackListItemResponse> CreateAsync(CreateTrackRequest request, CancellationToken ct = default)
    {
        var artistExists = await _db.Artists.AnyAsync(a => a.Id == request.ArtistId, ct);
        if (!artistExists)
        {
            throw new ValidationAppException($"Artist with id '{request.ArtistId}' does not exist.");
        }

        var isrcTaken = await _db.Tracks.AnyAsync(t => t.Isrc == request.Isrc, ct);
        if (isrcTaken)
        {
            throw new ValidationAppException($"ISRC '{request.Isrc}' is already assigned to another track.");
        }

        var track = new Track
        {
            Title = request.Title.Trim(),
            ArtistId = request.ArtistId,
            Isrc = request.Isrc.Trim(),
            ReleaseDate = DateTime.SpecifyKind(request.ReleaseDate, DateTimeKind.Utc),
            Genre = request.Genre.Trim(),
            Status = TrackStatus.Draft
        };

        _db.Tracks.Add(track);
        await _db.SaveChangesAsync(ct);

        var artist = await _db.Artists.AsNoTracking().FirstAsync(a => a.Id == track.ArtistId, ct);
        return MapToListItem(track, artist.Name);
    }

    public async Task<List<TrackListItemResponse>> GetAllAsync(TrackQueryParameters query, CancellationToken ct = default)
    {
        var tracks = _db.Tracks.AsNoTracking().Include(t => t.Artist).AsQueryable();

        if (query.ArtistId.HasValue)
        {
            tracks = tracks.Where(t => t.ArtistId == query.ArtistId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            tracks = tracks.Where(t => t.Genre.ToLower() == query.Genre.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<TrackStatus>(query.Status, true, out var status))
            {
                throw new ValidationAppException(
                    $"Status '{query.Status}' is invalid. Valid values: draft, submitted, distributed.");
            }
            tracks = tracks.Where(t => t.Status == status);
        }

        var result = await tracks
            .OrderByDescending(t => t.ReleaseDate)
            .Select(t => new TrackListItemResponse
            {
                Id = t.Id,
                Title = t.Title,
                ArtistId = t.ArtistId,
                ArtistName = t.Artist!.Name,
                Isrc = t.Isrc,
                ReleaseDate = t.ReleaseDate,
                Genre = t.Genre,
                Status = t.Status.ToString().ToLower()
            })
            .ToListAsync(ct);

        return result;
    }

    public async Task<TrackDetailResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var track = await _db.Tracks
            .AsNoTracking()
            .Include(t => t.Artist)
            .Include(t => t.Distributions)
                .ThenInclude(d => d.Dsp)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (track is null)
        {
            throw new NotFoundException(nameof(Track), id);
        }

        return MapToDetail(track);
    }

    public async Task<TrackDetailResponse> DistributeAsync(int id, DistributeTrackRequest request, CancellationToken ct = default)
    {
        var track = await _db.Tracks
            .Include(t => t.Artist)
            .Include(t => t.Distributions)
                .ThenInclude(d => d.Dsp)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (track is null)
        {
            throw new NotFoundException(nameof(Track), id);
        }

        var distinctDspIds = request.DspIds.Distinct().ToList();

        var validDspIds = await _db.Dsps
            .Where(d => distinctDspIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(ct);

        var invalidIds = distinctDspIds.Except(validDspIds).ToList();
        if (invalidIds.Count > 0)
        {
            throw new ValidationAppException($"Unknown DSP id(s): {string.Join(", ", invalidIds)}.");
        }

        var now = DateTime.UtcNow;
        foreach (var dspId in distinctDspIds)
        {
            var existing = track.Distributions.FirstOrDefault(d => d.DspId == dspId);
            if (existing is not null)
            {
                // Re-submitting resets it to pending rather than creating a duplicate row.
                existing.SubmittedAt = now;
                existing.Status = DistributionStatus.Pending;
            }
            else
            {
                _db.TrackDistributions.Add(new TrackDistribution
                {
                    TrackId = track.Id,
                    DspId = dspId,
                    SubmittedAt = now,
                    Status = DistributionStatus.Pending
                });
            }
        }

        if (track.Status == TrackStatus.Draft)
        {
            track.Status = TrackStatus.Submitted;
        }

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<TrackListItemResponse> UpdateStatusAsync(int id, UpdateTrackStatusRequest request, CancellationToken ct = default)
    {
        var track = await _db.Tracks.Include(t => t.Artist).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (track is null)
        {
            throw new NotFoundException(nameof(Track), id);
        }

        // RegularExpression validation on the DTO already guarantees this parses.
        Enum.TryParse<TrackStatus>(request.Status, true, out var newStatus);

        if (newStatus == track.Status)
        {
            throw new ValidationAppException(
                $"Track {id} is already '{ToLowerName(track.Status)}'.");
        }

        var allowedNext = AllowedStatusTransitions[track.Status];
        if (!allowedNext.Contains(newStatus))
        {
            var allowedDescription = allowedNext.Length > 0
                ? string.Join(", ", allowedNext.Select(ToLowerName))
                : "none - this is a terminal status";

            throw new ValidationAppException(
                $"Cannot change status from '{ToLowerName(track.Status)}' to '{ToLowerName(newStatus)}'. " +
                $"Allowed next status: {allowedDescription}.");
        }

        track.Status = newStatus;

        await _db.SaveChangesAsync(ct);

        return MapToListItem(track, track.Artist!.Name);
    }

    private static string ToLowerName(TrackStatus status) => status.ToString().ToLower();

    private static TrackListItemResponse MapToListItem(Track t, string artistName) => new()
    {
        Id = t.Id,
        Title = t.Title,
        ArtistId = t.ArtistId,
        ArtistName = artistName,
        Isrc = t.Isrc,
        ReleaseDate = t.ReleaseDate,
        Genre = t.Genre,
        Status = t.Status.ToString().ToLower()
    };

    private static TrackDetailResponse MapToDetail(Track t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        ArtistId = t.ArtistId,
        ArtistName = t.Artist!.Name,
        Isrc = t.Isrc,
        ReleaseDate = t.ReleaseDate,
        Genre = t.Genre,
        Status = t.Status.ToString().ToLower(),
        Distributions = t.Distributions.Select(d => new TrackDistributionResponse
        {
            Id = d.Id,
            DspId = d.DspId,
            DspName = d.Dsp!.Name,
            SubmittedAt = d.SubmittedAt,
            Status = d.Status.ToString().ToLower()
        }).ToList()
    };
}
