namespace AetherPlayer.Models;

public class Track
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string Genre { get; set; } = string.Empty;
    public List<string> MoodTags { get; set; } = [];
    public string Lyrics { get; set; } = string.Empty;
    public string CoverPath { get; set; } = string.Empty;
    public string AudioPath { get; set; } = string.Empty;
    public DateTime AddedOn { get; set; }
    public bool IsFavorite { get; set; }
    public int Rating { get; set; }
    public bool IsMissing { get; set; }

    public string DurationDisplay => $"{(int)Duration.TotalMinutes}:{Duration.Seconds:D2}";
    public string MoodTagsDisplay => string.Join(" • ", MoodTags);
}
