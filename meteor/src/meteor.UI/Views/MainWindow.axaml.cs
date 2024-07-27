using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.UI.Controls;
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

        var fileExplorerSidebar = new FileExplorerControl();

        var gridSplitter = new GridSplitter
        {
            Width = 1,
            MinWidth = 1,
            MaxWidth = 1,
            Background = new SolidColorBrush(Colors.Gray),
            ResizeDirection = GridResizeDirection.Columns
        };

        var horizontalSplit = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("200,Auto,*")
        };

        Grid.SetColumn(fileExplorerSidebar, 0);
        Grid.SetColumn(gridSplitter, 1);
        Grid.SetColumn(tabControl, 2);

        horizontalSplit.Children.Add(fileExplorerSidebar);
        horizontalSplit.Children.Add(gridSplitter);
        horizontalSplit.Children.Add(tabControl);

        Content = horizontalSplit;
    }
}