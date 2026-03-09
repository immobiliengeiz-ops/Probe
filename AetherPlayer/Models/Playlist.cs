namespace AetherPlayer.Models;

public class Playlist
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CoverPath { get; set; } = string.Empty;
    public List<Track> Tracks { get; set; } = [];

    public int TrackCount => Tracks.Count;
    public string DurationDisplay => $"{Tracks.Sum(x => x.Duration.TotalMinutes):0} min";
}
