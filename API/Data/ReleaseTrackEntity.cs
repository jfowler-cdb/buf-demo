namespace API.Data;

public class ReleaseTrackEntity
{
    public string ReleaseId { get; set; } = string.Empty;
    public string TrackId { get; set; } = string.Empty;

    public ReleaseEntity Release { get; set; } = null!;
    public TrackEntity Track { get; set; } = null!;
}
