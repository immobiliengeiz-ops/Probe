using AetherPlayer.Models;

namespace AetherPlayer.Services;

public interface IAppStateService
{
    AppState Load();
    void Save(AppState state);
}
