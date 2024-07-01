using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using meteor.ViewModels;

namespace meteor.Views;

public partial class TitleBar : UserControl
{
    public TitleBar()
    {
        InitializeComponent();
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // Find the main window
        if (this.GetVisualRoot() is Window { DataContext: MainWindowViewModel mainViewModel })
            mainViewModel.OpenFolderCommand.Execute();
    }
}