namespace MusicDistribution.Application.DTOs;

public class DspResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TrackDistributionResponse
{
    public int Id { get; set; }
    public int DspId { get; set; }
    public string DspName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
