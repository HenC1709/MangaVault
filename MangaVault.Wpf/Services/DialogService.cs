using System.Diagnostics;
using System.IO;
using System.Windows;
using MangaVault.Wpf.Views;
using Microsoft.Win32;

namespace MangaVault.Wpf.Services;

public interface IDialogService
{
    bool Confirm(string title, string message, string confirmText = "Confirmar", bool isDestructive = false);
    string? PickImageFile();
}

public interface IShellLauncher
{
    void OpenFolder(string path);
}

public sealed class DialogService : IDialogService
{
    public bool Confirm(string title, string message, string confirmText = "Confirmar", bool isDestructive = false)
    {
        var dialog = new ConfirmDialog(title, message, confirmText, isDestructive);
        Window? owner = System.Windows.Application.Current?.MainWindow;
        if (owner is not null && owner.IsLoaded)
            dialog.Owner = owner;
        else
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        return dialog.ShowDialog() == true;
    }

    public string? PickImageFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar portada",
            Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}

public sealed class ShellLauncher : IShellLauncher
{
    public void OpenFolder(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = true
        });
    }
}
