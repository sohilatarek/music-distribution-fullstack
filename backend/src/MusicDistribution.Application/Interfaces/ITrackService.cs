using MusicDistribution.Application.DTOs;

namespace MusicDistribution.Application.Interfaces;

public interface ITrackService
{
    Task<TrackListItemResponse> CreateAsync(CreateTrackRequest request, CancellationToken ct = default);
    Task<List<TrackListItemResponse>> GetAllAsync(TrackQueryParameters query, CancellationToken ct = default);
    Task<TrackDetailResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TrackDetailResponse> DistributeAsync(int id, DistributeTrackRequest request, CancellationToken ct = default);
    Task<TrackListItemResponse> UpdateStatusAsync(int id, UpdateTrackStatusRequest request, CancellationToken ct = default);
}
