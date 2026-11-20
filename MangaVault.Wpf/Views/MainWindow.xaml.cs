using System.Windows;
using MangaVault.Wpf.ViewModels;

namespace MangaVault.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
