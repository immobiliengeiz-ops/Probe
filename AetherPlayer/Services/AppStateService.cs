using System.IO;
using System.Text.Json;
using AetherPlayer.Models;

namespace AetherPlayer.Services;

public class AppStateService : IAppStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _statePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AetherPlayer",
        "appstate.json");

    public AppState Load()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                return new AppState();
            }

            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public void Save(AppState state)
    {
        var directory = Path.GetDirectoryName(_statePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_statePath, json);
    }
}
