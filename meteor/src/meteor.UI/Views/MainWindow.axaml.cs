using Avalonia;
using Avalonia.Controls;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.UI.Interfaces.Services.Editor;
using meteor.UI.ViewModels;
using TabControl = meteor.UI.Controls.TabControl;
namespace meteor.UI.Views;
public partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel mainWindowViewModel,
        ITabService tabService,
        IEditorLayoutManager layoutManager,
        IEditorInputHandler inputHandler,
        ITextMeasurer textMeasurer,
        IEditorConfig config,
        IScrollManager scrollManager,
        IPointerEventHandler pointerEventHandler)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;
        this.AttachDevTools();
        
        var tabControl = new TabControl(tabService, scrollManager, layoutManager, inputHandler,
            pointerEventHandler, textMeasurer, config);
        Content = tabControl;
    }
}