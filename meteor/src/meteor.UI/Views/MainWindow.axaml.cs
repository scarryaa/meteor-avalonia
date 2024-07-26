using Avalonia.Controls;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Controls;
using meteor.UI.Interfaces.Services.Editor;
using meteor.UI.ViewModels;

namespace meteor.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel mainWindowViewModel,
        IEditorViewModel editorViewModel,
        IEditorLayoutManager layoutManager,
        IEditorInputHandler inputHandler,
        ITextMeasurer textMeasurer,
        IEditorConfig config,
        IScrollManager scrollManager,
        IPointerEventHandler pointerEventHandler)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;

        var editorControl = new EditorControl(
            editorViewModel,
            scrollManager,
            layoutManager,
            inputHandler,
            pointerEventHandler,
            textMeasurer,
            config
        );
        Content = editorControl;
    }
}