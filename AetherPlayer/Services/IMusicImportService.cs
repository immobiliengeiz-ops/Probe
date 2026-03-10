using AetherPlayer.Models;

namespace AetherPlayer.Services;

public interface IMusicImportService
{
    IReadOnlyList<Track> LoadFromFolder(string folderPath, bool recursive = true);
    Track? LoadFromFile(string filePath);
    bool IsSupportedAudioFile(string filePath);
}
