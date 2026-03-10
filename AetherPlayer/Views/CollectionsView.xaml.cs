using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AetherPlayer.ViewModels;

namespace AetherPlayer.Views;

public partial class CollectionsView : UserControl
{
    public CollectionsView()
    {
        InitializeComponent();
    }

    private void CollectionTracksList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ListView list)
        {
            vm.SetSelectedTracks(list.SelectedItems.Cast<object>().ToList());
        }
    }

    private void CollectionTracksList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ListView { SelectedItem: TrackViewModel track })
        {
            vm.PlayTrackCommand.Execute(track);
        }
    }
}
