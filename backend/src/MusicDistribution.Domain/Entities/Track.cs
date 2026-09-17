using MusicDistribution.Domain.Enums;

namespace MusicDistribution.Domain.Entities;

public class Track
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ArtistId { get; set; }
    public Artist? Artist { get; set; }

    /// <summary>International Standard Recording Code - unique per track.</summary>
    public string Isrc { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; } = string.Empty;
    public TrackStatus Status { get; set; } = TrackStatus.Draft;

    public ICollection<TrackDistribution> Distributions { get; set; } = new List<TrackDistribution>();
}
