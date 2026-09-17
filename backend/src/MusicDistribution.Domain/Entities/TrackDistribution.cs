using MusicDistribution.Domain.Enums;

namespace MusicDistribution.Domain.Entities;

public class TrackDistribution
{
    public int Id { get; set; }
    public int TrackId { get; set; }
    public Track? Track { get; set; }
    public int DspId { get; set; }
    public Dsp? Dsp { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DistributionStatus Status { get; set; } = DistributionStatus.Pending;
}
