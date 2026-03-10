namespace AetherPlayer.Models;

public class StoredPlaylist
{
    public string Name { get; set; } = string.Empty;
    public List<string> TrackIds { get; set; } = [];
}
