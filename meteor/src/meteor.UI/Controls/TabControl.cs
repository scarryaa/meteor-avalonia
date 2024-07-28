using Avalonia.Controls;
using Avalonia.Threading;
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
    private bool _isActiveTabChanging;
    private ITabViewModel _lastSelectedTab;
    private bool _isUpdatingActiveTab;

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
        _tabService.TabAdded += (_, newTab) =>
        {
            _tabService.SetActiveTab(newTab);
        };
        _tabService.TabRemoved += (_, _) =>
        {
            if (_tabService.Tabs.Count == 0)
            {
                _contentArea.Content = null;
                _tabService.SetActiveTab(null);
            }
        };
        _tabService.ActiveTabChanged += (_, _) => UpdateActiveTab();
        
        var debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        debounceTimer.Tick += (_, _) =>
        {
            debounceTimer.Stop();
            if (!_isActiveTabChanging && _tabStrip.SelectedItem is ITabViewModel selectedTab &&
                selectedTab != _lastSelectedTab)
            {
                _lastSelectedTab = selectedTab;
                _tabService.SetActiveTab(selectedTab);
            }

            _isActiveTabChanging = false;
        };

        _tabStrip.SelectionChanged += (_, _) =>
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        };
    }

    private void UpdateActiveTab()
    {
        if (_isUpdatingActiveTab) return;
        _isUpdatingActiveTab = true;

        try
        {
            var previousTab = _tabService.GetPreviousActiveTab();
            StoreCurrentTabContent();
            SaveCurrentTabScrollPosition(previousTab);

            var activeTab = _tabService.ActiveTab;
            if (_tabStrip.SelectedItem != activeTab) _tabStrip.SelectedItem = activeTab;

            if (activeTab != null)
            {
                var editorControl = CreateEditorControl(activeTab.EditorViewModel);
                if (editorControl != null)
                {
                    activeTab.EditorViewModel.LoadContent(activeTab.Content);
                    _contentArea.Content = editorControl;
                    RestoreTabScrollPosition(activeTab);
                }
            }
            else
            {
                _contentArea.Content = null;
            }
        }
        finally
        {
            _isUpdatingActiveTab = false;
        }
    }

    private void SaveCurrentTabScrollPosition(ITabViewModel previousTab)
    {
        if (_contentArea.Content is EditorControl editorControl)
            if (previousTab != null)
                previousTab.SaveScrollPosition(editorControl.GetScrollPositionX(), editorControl.GetScrollPositionY());
    }

    private void RestoreTabScrollPosition(ITabViewModel tab)
    {
        if (_contentArea.Content is EditorControl editorControl)
            editorControl.SetScrollPosition(tab.ScrollPositionX, tab.ScrollPositionY);
    }

    private void StoreCurrentTabContent()
    {
        if (_contentArea.Content is EditorControl editorControl &&
            editorControl.DataContext is EditorViewModel viewModel)
        {
            var currentTab = _tabService.Tabs.FirstOrDefault(t => t.EditorViewModel == viewModel);
            if (currentTab != null) currentTab.Content = viewModel.Content;
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