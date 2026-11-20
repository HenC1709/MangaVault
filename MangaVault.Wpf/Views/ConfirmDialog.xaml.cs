using System.Windows;

namespace MangaVault.Wpf.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(
        string title,
        string message,
        string confirmText = "Confirmar",
        bool isDestructive = false)
    {
        InitializeComponent();

        Title = title;
        TitleBlock.Text = title;
        MessageBlock.Text = message;
        ConfirmButton.Content = confirmText;

        if (isDestructive && TryFindResource("DestructiveButtonStyle") is Style destructiveStyle)
        {
            ConfirmButton.Style = destructiveStyle;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
