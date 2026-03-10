using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using AetherPlayer.ViewModels;

namespace AetherPlayer.Views;

public partial class BottomPlayerBar : UserControl
{
    private bool _isDragging;

    public BottomPlayerBar()
    {
        InitializeComponent();
    }

    private void ProgressSlider_OnDragStarted(object sender, DragStartedEventArgs e)
    {
        _isDragging = true;
        if (DataContext is MainViewModel vm)
        {
            vm.BeginSeekInteraction();
        }
    }

    private void ProgressSlider_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (sender is not Slider slider || DataContext is not MainViewModel vm)
        {
            _isDragging = false;
            return;
        }

        vm.CommitSeekInteraction(slider.Value);
        _isDragging = false;
    }

    private void ProgressSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isDragging || DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.UpdateSeekPreview(e.NewValue);
    }

    private void ProgressSlider_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.BeginSeekInteraction();
        }
    }

    private void ProgressSlider_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider || DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.CommitSeekInteraction(slider.Value);
    }
}
