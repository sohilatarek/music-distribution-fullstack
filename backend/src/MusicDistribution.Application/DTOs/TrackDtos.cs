using System.ComponentModel.DataAnnotations;

namespace MusicDistribution.Application.DTOs;

public class CreateTrackRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "ArtistId is required.")]
    public int ArtistId { get; set; }

    [Required(ErrorMessage = "Isrc is required.")]
    [MaxLength(20)]
    public string Isrc { get; set; } = string.Empty;

    [Required(ErrorMessage = "ReleaseDate is required.")]
    public DateTime ReleaseDate { get; set; }

    [Required(ErrorMessage = "Genre is required.")]
    [MaxLength(100)]
    public string Genre { get; set; } = string.Empty;
}

public class TrackListItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ArtistId { get; set; }
    public string ArtistName { get; set; } = string.Empty;
    public string Isrc { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class TrackDetailResponse : TrackListItemResponse
{
    public List<TrackDistributionResponse> Distributions { get; set; } = new();
}

public class DistributeTrackRequest
{
    [Required(ErrorMessage = "At least one DspId is required.")]
    [MinLength(1, ErrorMessage = "At least one DspId is required.")]
    public List<int> DspIds { get; set; } = new();
}

public class UpdateTrackStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(draft|submitted|distributed)$",
        ErrorMessage = "Status must be one of: draft, submitted, distributed.")]
    public string Status { get; set; } = string.Empty;
}

public class TrackQueryParameters
{
    public int? ArtistId { get; set; }
    public string? Genre { get; set; }
    public string? Status { get; set; }
}
