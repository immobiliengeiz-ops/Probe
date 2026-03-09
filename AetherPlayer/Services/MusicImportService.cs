using System.IO;
using System.Security.Cryptography;
using System.Text;
using AetherPlayer.Models;
using TagFile = TagLib.File;
using IoFile = System.IO.File;

namespace AetherPlayer.Services;

public class MusicImportService : IMusicImportService
{
    private static readonly string[] Supported = [".mp3", ".wav", ".flac", ".m4a", ".aac", ".wma", ".ogg"];

    public IReadOnlyList<Track> LoadFromFolder(string folderPath, bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return [];
        }

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(folderPath, "*.*", option)
            .Where(IsSupportedAudioFile)
            .Take(5000)
            .ToList();

        return files.Select(LoadFromFile).Where(x => x is not null).Cast<Track>().ToList();
    }

    public bool IsSupportedAudioFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return Supported.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    public Track? LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !IoFile.Exists(filePath) || !IsSupportedAudioFile(filePath))
        {
            return null;
        }

        return BuildTrackFromFile(filePath);
    }

    private static Track BuildTrackFromFile(string audioPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(audioPath);
        var defaultTitle = string.IsNullOrWhiteSpace(fileName) ? "Unknown Title" : fileName;
        var defaultArtist = "Unknown Artist";
        var defaultAlbum = "Unknown Album";
        var defaultGenre = "Unknown";

        try
        {
            using var media = TagFile.Create(audioPath);
            var title = string.IsNullOrWhiteSpace(media.Tag.Title) ? defaultTitle : media.Tag.Title;
            var artist = media.Tag.FirstPerformer ?? defaultArtist;
            var album = string.IsNullOrWhiteSpace(media.Tag.Album) ? defaultAlbum : media.Tag.Album;
            var genre = media.Tag.FirstGenre ?? defaultGenre;
            var duration = media.Properties.Duration > TimeSpan.Zero ? media.Properties.Duration : TimeSpan.FromMinutes(3);
            var lyrics = media.Tag.Lyrics ?? string.Empty;
            var coverPath = ExtractCoverArt(media, audioPath) ?? FindCoverFor(audioPath);

            return new Track
            {
                Id = $"local-{ComputeHash(audioPath)}",
                Title = title,
                Artist = artist,
                Album = album,
                Genre = genre,
                MoodTags = ["Imported"],
                Duration = duration,
                AddedOn = IoFile.GetCreationTime(audioPath),
                Lyrics = lyrics,
                AudioPath = audioPath,
                CoverPath = coverPath,
                IsFavorite = false,
                Rating = 0
            };
        }
        catch
        {
            return new Track
            {
                Id = $"local-{ComputeHash(audioPath)}",
                Title = defaultTitle,
                Artist = defaultArtist,
                Album = defaultAlbum,
                Genre = defaultGenre,
                MoodTags = ["Imported"],
                Duration = TimeSpan.FromMinutes(3),
                AddedOn = IoFile.GetCreationTime(audioPath),
                Lyrics = string.Empty,
                AudioPath = audioPath,
                CoverPath = FindCoverFor(audioPath),
                IsFavorite = false,
                Rating = 0
            };
        }
    }

    private static string? ExtractCoverArt(TagFile media, string audioPath)
    {
        var picture = media.Tag.Pictures?.FirstOrDefault();
        if (picture is null || picture.Data is null || picture.Data.Count == 0)
        {
            return null;
        }

        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AetherPlayer",
            "CoverCache");
        Directory.CreateDirectory(cacheDir);

        var file = Path.Combine(cacheDir, $"{ComputeHash(audioPath)}.jpg");
        if (!IoFile.Exists(file))
        {
            IoFile.WriteAllBytes(file, picture.Data.Data);
        }

        return file;
    }

    private static string FindCoverFor(string audioPath)
    {
        var basePath = Path.Combine(Path.GetDirectoryName(audioPath) ?? string.Empty, Path.GetFileNameWithoutExtension(audioPath));
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
        {
            var candidate = basePath + ext;
            if (IoFile.Exists(candidate))
            {
                return candidate;
            }
        }

        return "Assets/Covers/cover1.png";
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(value.ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
