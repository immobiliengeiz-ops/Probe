namespace AetherPlayer.Services;

public interface ISelectionDialogService
{
    string? ChoosePlaylist(IEnumerable<string> existingNames);
    string? ChooseCollection(IEnumerable<string> existingNames);
}
