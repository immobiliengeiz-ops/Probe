namespace AetherPlayer.Models;

public class Album
{
    public string Name { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string CoverPath { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public List<Track> Tracks { get; set; } = [];
}
