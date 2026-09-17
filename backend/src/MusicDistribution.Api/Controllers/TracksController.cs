using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicDistribution.Application.DTOs;
using MusicDistribution.Application.Interfaces;

namespace MusicDistribution.Api.Controllers;

[ApiController]
[Route("api/tracks")]
public class TracksController : ControllerBase
{
    private readonly ITrackService _trackService;

    public TracksController(ITrackService trackService)
    {
        _trackService = trackService;
    }

    /// <summary>Create a track for an artist.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TrackListItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTrackRequest request, CancellationToken ct)
    {
        var track = await _trackService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = track.Id }, track);
    }

    /// <summary>List tracks, optionally filtered by artistId, genre and/or status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TrackListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? artistId,
        [FromQuery] string? genre,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var query = new TrackQueryParameters { ArtistId = artistId, Genre = genre, Status = status };
        var tracks = await _trackService.GetAllAsync(query, ct);
        return Ok(tracks);
    }

    /// <summary>Get a single track, including its DSP distribution statuses.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TrackDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var track = await _trackService.GetByIdAsync(id, ct);
        return Ok(track);
    }

    /// <summary>Submit a track to one or more DSPs. Requires authentication.</summary>
    [HttpPost("{id:int}/distribute")]
    [Authorize]
    [ProducesResponseType(typeof(TrackDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Distribute(int id, [FromBody] DistributeTrackRequest request, CancellationToken ct)
    {
        var track = await _trackService.DistributeAsync(id, request, ct);
        return Ok(track);
    }

    /// <summary>Update a track's status. Requires authentication.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize]
    [ProducesResponseType(typeof(TrackListItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTrackStatusRequest request, CancellationToken ct)
    {
        var track = await _trackService.UpdateStatusAsync(id, request, ct);
        return Ok(track);
    }
}
