using Avalonia.Controls;
using meteor.Core.Interfaces.Services;
using meteor.UI.Controls;
using meteor.UI.ViewModels;

namespace meteor.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel mainWindowViewModel, EditorViewModel editorViewModel,
        ITextMeasurer textMeasurer, ITextBufferService textBufferService)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;

        var editorControl = new EditorControl(editorViewModel, textMeasurer, textBufferService);
        Content = editorControl;
    }
}