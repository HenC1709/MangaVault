using System.Windows;
using System.Windows.Controls;
using MangaVault.Wpf.ViewModels;
using MangaVault.Wpf.ViewModels.Models;

namespace MangaVault.Wpf.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm)
            return;

        var input = new ChangePasswordInput(
            CurrentPasswordInput.Password,
            NewPasswordInput.Password,
            ConfirmPasswordInput.Password);

        await vm.ChangePasswordCommand.ExecuteAsync(input);

        if (!vm.HasPasswordError)
        {
            CurrentPasswordInput.Clear();
            NewPasswordInput.Clear();
            ConfirmPasswordInput.Clear();
        }
    }
}
