using Microsoft.AspNetCore.Mvc;
using MusicDistribution.Application.DTOs;
using MusicDistribution.Application.Interfaces;

namespace MusicDistribution.Api.Controllers;

[ApiController]
[Route("api/artists")]
public class ArtistsController : ControllerBase
{
    private readonly IArtistService _artistService;

    public ArtistsController(IArtistService artistService)
    {
        _artistService = artistService;
    }

    /// <summary>Create a new artist.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ArtistResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateArtistRequest request, CancellationToken ct)
    {
        var artist = await _artistService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { }, artist);
    }

    /// <summary>List all artists.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ArtistResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var artists = await _artistService.GetAllAsync(ct);
        return Ok(artists);
    }
}
