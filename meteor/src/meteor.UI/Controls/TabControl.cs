using Avalonia.Controls;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Interfaces.Services.Editor;
using meteor.UI.ViewModels;

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
        _tabService = tabService ?? throw new ArgumentNullException(nameof(tabService));
        _scrollManager = scrollManager ?? throw new ArgumentNullException(nameof(scrollManager));
        _layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
        _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
        _pointerEventHandler = pointerEventHandler ?? throw new ArgumentNullException(nameof(pointerEventHandler));
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        _config = config ?? throw new ArgumentNullException(nameof(config));

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
        _tabService.TabAdded += (_, _) => UpdateTabs();
        _tabService.TabRemoved += (_, _) =>
        {
            UpdateTabs();
            if (_tabService.Tabs.Count == 0) _tabService.SetActiveTab(null);
        };
        _tabService.ActiveTabChanged += (_, _) => UpdateActiveTab();
        _tabStrip.SelectionChanged += (sender, _) =>
        {
            if (sender is HorizontalScrollableTabControl tabControl &&
                tabControl.SelectedItem is ITabViewModel selectedTab)
                _tabService.SetActiveTab(selectedTab);
        };
    }

    private void UpdateTabs()
    {
        var previousSelectedItem = _tabStrip.SelectedItem;
        _tabStrip.ItemsSource = null;
        _tabStrip.ItemsSource = _tabService.Tabs;

        if (_tabService.Tabs.Contains(previousSelectedItem))
        {
            _tabStrip.SelectedItem = previousSelectedItem;
        }
        else
        {
            if (_tabService.Tabs.Count == 0)
            {
                _contentArea.Content = null;
            }
            else
            {
                if (_tabService.ActiveTab == null) _contentArea.Content = null;
                UpdateActiveTab();
            }
        }
    }

    private void StoreCurrentTabContent()
    {
        var currentTab = _tabService.ActiveTab;
        if (currentTab != null && _contentArea.Content is EditorControl editorControl)
            if (editorControl.DataContext is EditorViewModel viewModel)
                currentTab.Content = viewModel.Content;
    }

    private void UpdateActiveTab()
    {
        StoreCurrentTabContent();

        var activeTab = _tabService.ActiveTab;
        if (_tabStrip.SelectedItem != activeTab) _tabStrip.SelectedItem = activeTab;

        if (activeTab != null)
        {
            var editorControl = CreateEditorControl(activeTab.EditorViewModel);
            if (editorControl != null)
            {
                activeTab.EditorViewModel.Content = activeTab.Content;
                _contentArea.Content = editorControl;
            }
        }
        else
        {
            _contentArea.Content = null;
        }
    }

    private EditorControl CreateEditorControl(IEditorViewModel viewModel)
    {
        if (viewModel == null) return null;

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