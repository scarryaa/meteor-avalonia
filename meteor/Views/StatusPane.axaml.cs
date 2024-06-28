using Avalonia.Controls;
using meteor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace meteor.Views;

public partial class StatusPane : UserControl
{
    public StatusPane()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<StatusPaneViewModel>();
    }
}