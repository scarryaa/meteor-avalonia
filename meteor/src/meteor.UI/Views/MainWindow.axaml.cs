using Avalonia.Controls;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Controls;
using meteor.UI.ViewModels;

namespace meteor.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel mainWindowViewModel, IEditorViewModel editorViewModel,
        ITextMeasurer textMeasurer, IEditorConfig config, IScrollManager scrollManager)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;

        var editorControl = new EditorControl(editorViewModel, textMeasurer, config, scrollManager);
        Content = editorControl;
    }
}