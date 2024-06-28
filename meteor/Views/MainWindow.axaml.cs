using Avalonia;
using Avalonia.Controls;
using meteor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        DataContext = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
    }
}