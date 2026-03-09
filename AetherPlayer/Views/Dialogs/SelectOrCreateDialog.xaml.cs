using System.Windows;

namespace AetherPlayer.Views.Dialogs;

public partial class SelectOrCreateDialog : Window
{
    public SelectOrCreateDialog(string title, IEnumerable<string> existing)
    {
        InitializeComponent();
        DialogTitle.Text = title;
        ExistingItemsList.ItemsSource = existing.OrderBy(x => x).ToList();
        if (ExistingItemsList.Items.Count > 0)
        {
            ExistingItemsList.SelectedIndex = 0;
        }
    }

    public string? SelectedValue { get; private set; }

    private void Confirm_OnClick(object sender, RoutedEventArgs e)
    {
        var typed = NewNameTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(typed))
        {
            SelectedValue = typed;
            DialogResult = true;
            return;
        }

        if (ExistingItemsList.SelectedItem is string selected)
        {
            SelectedValue = selected;
            DialogResult = true;
        }
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
