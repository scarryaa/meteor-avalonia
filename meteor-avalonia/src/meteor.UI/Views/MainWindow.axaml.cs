using System;
using Avalonia.Controls;
using meteor.Core.Interfaces.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = serviceProvider.GetRequiredService<IMainWindowViewModel>();
    }
}