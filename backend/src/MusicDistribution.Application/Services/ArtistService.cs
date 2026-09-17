using Microsoft.EntityFrameworkCore;
using MusicDistribution.Application.DTOs;
using MusicDistribution.Application.Interfaces;
using MusicDistribution.Domain.Entities;

namespace MusicDistribution.Application.Services;

public class ArtistService : IArtistService
{
    private readonly IApplicationDbContext _db;

    public ArtistService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ArtistResponse> CreateAsync(CreateArtistRequest request, CancellationToken ct = default)
    {
        var artist = new Artist
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Country = request.Country.Trim()
        };

        _db.Artists.Add(artist);
        await _db.SaveChangesAsync(ct);

        return Map(artist);
    }

    public async Task<List<ArtistResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Artists
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new ArtistResponse
            {
                Id = a.Id,
                Name = a.Name,
                Email = a.Email,
                Country = a.Country
            })
            .ToListAsync(ct);
    }

    private static ArtistResponse Map(Artist a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Email = a.Email,
        Country = a.Country
    };
}
