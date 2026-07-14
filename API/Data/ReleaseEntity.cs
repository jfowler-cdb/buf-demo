namespace API.Data;

public class ReleaseEntity
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
}
