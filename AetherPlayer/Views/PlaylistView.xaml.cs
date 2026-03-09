using System.Windows.Controls;
using AetherPlayer.ViewModels;

namespace AetherPlayer.Views;

public partial class PlaylistView : UserControl
{
    public PlaylistView()
    {
        InitializeComponent();
    }

    private void PlaylistTracksList_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ListView { SelectedItem: TrackViewModel track })
        {
            vm.PlayTrackCommand.Execute(track);
        }
    }

    private void PlaylistTracksList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ListView list)
        {
            vm.SetSelectedTracks(list.SelectedItems);
        }
    }
}
