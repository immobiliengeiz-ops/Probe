using System.Windows;
using AetherPlayer.Views.Dialogs;

namespace AetherPlayer.Services;

public class SelectionDialogService : ISelectionDialogService
{
    public string? ChoosePlaylist(IEnumerable<string> existingNames)
    {
        var dialog = new SelectOrCreateDialog("Add to Playlist", existingNames)
        {
            Owner = Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true ? dialog.SelectedValue : null;
    }

    public string? ChooseCollection(IEnumerable<string> existingNames)
    {
        var dialog = new SelectOrCreateDialog("Add to Collection", existingNames)
        {
            Owner = Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true ? dialog.SelectedValue : null;
    }
}
