using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AetherPlayer.ViewModels;

namespace AetherPlayer.Views;

public partial class FavoritesView : UserControl
{
    public FavoritesView()
    {
        InitializeComponent();
    }

    private void TracksList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ListView { SelectedItem: TrackViewModel track })
        {
            vm.PlayTrackCommand.Execute(track);
        }
    }

    private void TracksList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ListView list)
        {
            vm.SetSelectedTracks(list.SelectedItems.Cast<object>().ToList());
        }
    }
}
