using Avalonia.Controls;
using meteor.UI.Controls;
using meteor.UI.ViewModels;

namespace meteor.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel mainWindowViewModel, EditorViewModel editorViewModel)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;

        var editorControl = new EditorControl(editorViewModel);
        Content = editorControl;
    }
}