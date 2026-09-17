using MusicDistribution.Application.DTOs;

namespace MusicDistribution.Application.Interfaces;

public interface IArtistService
{
    Task<ArtistResponse> CreateAsync(CreateArtistRequest request, CancellationToken ct = default);
    Task<List<ArtistResponse>> GetAllAsync(CancellationToken ct = default);
}
