using Microsoft.AspNetCore.Mvc;
using MusicDistribution.Application.DTOs;
using MusicDistribution.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MusicDistribution.Api.Controllers;

/// <summary>Read-only lookup of available DSPs (Spotify, Apple Music, YouTube...), used by the
/// front-end to build the "distribute to" selector.</summary>
[ApiController]
[Route("api/dsps")]
public class DspsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public DspsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DspResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var dsps = await _db.Dsps
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DspResponse { Id = d.Id, Name = d.Name })
            .ToListAsync(ct);

        return Ok(dsps);
    }
}
