namespace AetherPlayer.Models;

public class AppState
{
    public double Volume { get; set; } = 0.75;
    public string? LastTrackId { get; set; }
    public NavSection LastSection { get; set; } = NavSection.Home;
    public List<string> LibraryFolders { get; set; } = [];
    public Dictionary<string, int> RatingsByTrackId { get; set; } = [];
    public List<string> RecentlyPlayedTrackIds { get; set; } = [];
    public List<UserCollection> Collections { get; set; } = [];
    public List<StoredPlaylist> Playlists { get; set; } = [];
    public List<string> QueueTrackIds { get; set; } = [];
    public bool ShuffleEnabled { get; set; }
    public RepeatMode RepeatMode { get; set; } = RepeatMode.Off;
}
