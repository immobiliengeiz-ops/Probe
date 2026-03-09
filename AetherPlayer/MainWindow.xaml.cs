using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AetherPlayer.ViewModels;

namespace AetherPlayer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        vm.RequestSearchFocus += (_, _) => FocusSearchBox();
        DataContext = vm;
    }

    private void FocusSearchBox()
    {
        var textBox = FindTaggedTextBox(this, "GlobalSearchBox");
        if (textBox is null)
        {
            return;
        }

        textBox.Focus();
        textBox.SelectAll();
    }

    private void Window_OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void Window_OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            var normalized = files.Where(path => Directory.Exists(path) || File.Exists(path)).ToList();
            vm.HandleDropItems(normalized);
        }
    }

    private static TextBox? FindTaggedTextBox(DependencyObject parent, string tag)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TextBox box && Equals(box.Tag, tag) && box.IsVisible)
            {
                return box;
            }

            var nested = FindTaggedTextBox(child, tag);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
