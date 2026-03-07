using AetherPlayer.Models;

namespace AetherPlayer.MockData;

public static class DemoLibrary
{
    public static IReadOnlyList<Track> Tracks { get; } = BuildTracks();
    public static IReadOnlyList<Album> Albums { get; } = BuildAlbums();
    public static IReadOnlyList<Playlist> Playlists { get; } = BuildPlaylists();

    private static List<Track> BuildTracks()
    {
        return
        [
            NewTrack("Velvet Horizon", "Nocturne Vale", "Lumen Nights", 248, "Synthwave", ["Night Drive", "Euphoric"], true, -2, "cover1.png", "demo1.mp3"),
            NewTrack("Moonlit Signal", "Aster Bloom", "Lumen Nights", 214, "Dream Pop", ["Calm", "Floating"], false, -1, "cover2.png", "demo2.mp3"),
            NewTrack("Pulse in Glass", "Nocturne Vale", "Lumen Nights", 193, "Electronic", ["Focused", "Urban"], false, -7, "cover3.png", "demo3.mp3"),
            NewTrack("Afterglow Motel", "Crimson Atlas", "City Thread", 228, "Indie", ["Warm", "Melancholic"], true, -12, "cover4.png", "demo4.mp3"),
            NewTrack("Midnight Marble", "Serein", "City Thread", 271, "Ambient", ["Sleepy", "Soft"], false, -3, "cover5.png", "demo5.mp3"),
            NewTrack("Neon Cathedral", "Crimson Atlas", "City Thread", 236, "Alt Rock", ["Bold", "Driving"], false, -5, "cover6.png", "demo6.mp3"),
            NewTrack("Gold Echo", "Aria Cascade", "Opal Skies", 202, "Chill House", ["Relaxed", "Sunny"], true, -8, "cover7.png", "demo7.mp3"),
            NewTrack("Cerulean Veins", "Aria Cascade", "Opal Skies", 222, "House", ["Energetic", "Dance"], false, -16, "cover8.png", "demo8.mp3"),
            NewTrack("Silk Static", "Faint Harbour", "Opal Skies", 257, "Lo-Fi", ["Study", "Rainy"], true, -4, "cover9.png", "demo9.mp3"),
            NewTrack("Paper Constellations", "Faint Harbour", "Opal Skies", 238, "Lo-Fi", ["Calm", "Late Night"], false, -9, "cover10.png", "demo10.mp3"),
            NewTrack("Ivory Sparks", "Neve", "Glass Gardens", 211, "Soul", ["Confident", "Smooth"], false, -6, "cover11.png", "demo11.mp3"),
            NewTrack("Dusk Archive", "Neve", "Glass Gardens", 244, "R&B", ["Moody", "Reflective"], true, -10, "cover12.png", "demo12.mp3")
        ];
    }

    private static List<Album> BuildAlbums()
    {
        return
        [
            new Album
            {
                Name = "Lumen Nights",
                Artist = "Nocturne Vale",
                CoverPath = "Assets/Covers/cover1.png",
                ReleaseYear = 2023,
                Tracks = Tracks.Where(t => t.Album == "Lumen Nights").ToList()
            },
            new Album
            {
                Name = "Opal Skies",
                Artist = "Aria Cascade",
                CoverPath = "Assets/Covers/cover7.png",
                ReleaseYear = 2024,
                Tracks = Tracks.Where(t => t.Album == "Opal Skies").ToList()
            }
        ];
    }

    private static List<Playlist> BuildPlaylists()
    {
        return
        [
            new Playlist
            {
                Name = "Night Drive Luxe",
                Description = "Sleek synth and city glow energy.",
                CoverPath = "Assets/Covers/cover1.png",
                Tracks = Tracks.Where(t => t.MoodTags.Contains("Night Drive") || t.Genre is "Synthwave" or "Electronic").Take(5).ToList()
            },
            new Playlist
            {
                Name = "Focus Velvet",
                Description = "Low-noise concentration with style.",
                CoverPath = "Assets/Covers/cover9.png",
                Tracks = Tracks.Where(t => t.MoodTags.Contains("Study") || t.MoodTags.Contains("Focused") || t.Genre == "Lo-Fi").Take(5).ToList()
            },
            new Playlist
            {
                Name = "Golden Hour Ease",
                Description = "Warm and uplifting end-of-day selections.",
                CoverPath = "Assets/Covers/cover7.png",
                Tracks = Tracks.Where(t => t.MoodTags.Contains("Sunny") || t.MoodTags.Contains("Warm") || t.MoodTags.Contains("Relaxed")).Take(5).ToList()
            }
        ];
    }

    private static Track NewTrack(string title, string artist, string album, int seconds, string genre, List<string> moodTags, bool favorite, int addedDaysOffset, string coverFile, string audioFile)
    {
        return new Track
        {
            Id = $"demo-{title.ToLowerInvariant().Replace(" ", "-")}",
            Title = title,
            Artist = artist,
            Album = album,
            Duration = TimeSpan.FromSeconds(seconds),
            Genre = genre,
            MoodTags = moodTags,
            IsFavorite = favorite,
            Rating = favorite ? 4 : 3,
            AddedOn = DateTime.Today.AddDays(addedDaysOffset),
            CoverPath = $"Assets/Covers/{coverFile}",
            AudioPath = $"Assets/Audio/{audioFile}",
            Lyrics = $"{title}\n\nIn the hush of electric midnight, {artist} writes silver stories over velvet drums.\nThis is demo lyric text for the Aether Player MVP."
        };
    }
}
