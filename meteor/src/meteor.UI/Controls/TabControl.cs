using Avalonia.Controls;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Interfaces.Services.Editor;

namespace meteor.UI.Controls;

public class TabControl : UserControl
{
    private readonly ITabService _tabService;
    private readonly IScrollManager _scrollManager;
    private readonly IEditorLayoutManager _layoutManager;
    private readonly IEditorInputHandler _inputHandler;
    private readonly IPointerEventHandler _pointerEventHandler;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;
    private readonly HorizontalScrollableTabControl _tabStrip;
    private readonly ContentControl _contentArea;

    public TabControl(ITabService tabService, IScrollManager scrollManager, IEditorLayoutManager layoutManager,
        IEditorInputHandler inputHandler, IPointerEventHandler pointerEventHandler, ITextMeasurer textMeasurer,
        IEditorConfig config)
    {
        _tabService = tabService;
        _scrollManager = scrollManager;
        _layoutManager = layoutManager;
        _inputHandler = inputHandler;
        _pointerEventHandler = pointerEventHandler;
        _textMeasurer = textMeasurer;
        _config = config;

        _tabStrip = new HorizontalScrollableTabControl
        {
            ItemsSource = _tabService.Tabs
        };

        _contentArea = new ContentControl();

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        Grid.SetRow(_tabStrip, 0);
        Grid.SetRow(_contentArea, 1);

        grid.Children.Add(_tabStrip);
        grid.Children.Add(_contentArea);

        Content = grid;

        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        _tabService.TabAdded += (sender, tab) => UpdateTabs();
        _tabService.TabRemoved += (sender, tab) => UpdateTabs();
        _tabService.ActiveTabChanged += (sender, tab) => UpdateActiveTab();
        _tabStrip.SelectionChanged += (sender, e) =>
        {
            if (_tabStrip.SelectedItem is ITabViewModel selectedTab) _tabService.SetActiveTab(selectedTab);
        };
    }

    private void UpdateTabs()
    {
        _tabStrip.ItemsSource = null;
        _tabStrip.ItemsSource = _tabService.Tabs;
        UpdateActiveTab();
    }

    private void UpdateActiveTab()
    {
        var activeTab = _tabService.ActiveTab;
        _tabStrip.SelectedItem = activeTab;
        _contentArea.Content = activeTab != null ? CreateEditorControl(activeTab.EditorViewModel) : null;
    }

    private EditorControl CreateEditorControl(IEditorViewModel viewModel)
    {
        return new EditorControl(
            viewModel,
            _scrollManager,
            _layoutManager,
            _inputHandler,
            _pointerEventHandler,
            _textMeasurer,
            _config
        );
    }
}