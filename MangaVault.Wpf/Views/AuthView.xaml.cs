using System.Windows;
using System.Windows.Controls;
using MangaVault.Wpf.ViewModels;
using MangaVault.Wpf.ViewModels.Models;

namespace MangaVault.Wpf.Views;

public partial class AuthView : UserControl
{
    public AuthView() => InitializeComponent();

    private void SwitchMode_Click(object sender, RoutedEventArgs e)
    {
        PasswordInput.Clear();
        ConfirmPasswordInput.Clear();
    }

    private void ToggleMode_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AuthViewModel vm)
            return;

        vm.SwitchModeCommand.Execute(!vm.IsRegisterMode);
        PasswordInput.Clear();
        ConfirmPasswordInput.Clear();
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AuthViewModel vm)
            return;

        var input = new AuthPasswordInput(PasswordInput.Password, ConfirmPasswordInput.Password);
        await vm.SubmitCommand.ExecuteAsync(input);

        if (!vm.HasError)
        {
            PasswordInput.Clear();
            ConfirmPasswordInput.Clear();
        }
    }
}
