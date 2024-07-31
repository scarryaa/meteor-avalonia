using Avalonia.Controls;
using Avalonia.Threading;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.UI.Config;
using meteor.UI.Features.Editor.Controls;
using meteor.UI.Features.Editor.Interfaces;
using meteor.UI.Features.Editor.ViewModels;

namespace meteor.UI.Features.Tabs.Controls;

public class TabControl : UserControl
{
    private readonly AvaloniaEditorConfig _avaloniaConfig;
    private readonly IEditorControlFactory _editorControlFactory;
    private readonly ITabService _tabService;
    private readonly IThemeManager _themeManager;
    private ContentControl _contentArea;
    private bool _isUpdatingActiveTab;
    private ITabViewModel _lastSelectedTab;
    private HorizontalScrollableTabControl _tabStrip;

    public TabControl(ITabService tabService, IEditorControlFactory editorControlFactory, IThemeManager themeManager)
    {
        _tabService = tabService ?? throw new ArgumentNullException(nameof(tabService));
        _editorControlFactory = editorControlFactory ?? throw new ArgumentNullException(nameof(editorControlFactory));
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        _avaloniaConfig = new AvaloniaEditorConfig(_themeManager);

        InitializeLayout();
        SetupEventHandlers();
    }

    private void InitializeLayout()
    {
        _tabStrip = new HorizontalScrollableTabControl
        {
            ItemsSource = _tabService.Tabs,
            Background = _avaloniaConfig.TabBackgroundBrush,
            Foreground = _avaloniaConfig.TabForegroundBrush
        };

        _contentArea = new ContentControl();

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Background = _avaloniaConfig.BackgroundBrush
        };

        Grid.SetRow(_tabStrip, 0);
        Grid.SetRow(_contentArea, 1);

        grid.Children.Add(_tabStrip);
        grid.Children.Add(_contentArea);

        Content = grid;
    }

    private void SetupEventHandlers()
    {
        _tabService.TabAdded += (_, newTab) => _tabService.SetActiveTab(newTab);
        _tabService.TabRemoved += OnTabRemoved;
        _tabService.ActiveTabChanged += (_, _) => UpdateActiveTab();
        _themeManager.ThemeChanged += OnThemeChanged;

        SetupTabStripSelectionHandler();
    }

    private void OnTabRemoved(object sender, ITabViewModel e)
    {
        if (_tabService.Tabs.Count == 0)
        {
            _contentArea.Content = null;
            _tabService.SetActiveTab(null);
        }
    }

    private void SetupTabStripSelectionHandler()
    {
        var debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        debounceTimer.Tick += (_, _) => HandleTabSelection(debounceTimer);

        _tabStrip.SelectionChanged += (_, _) =>
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        };
    }

    private void HandleTabSelection(DispatcherTimer debounceTimer)
    {
        debounceTimer.Stop();
        if (_tabStrip.SelectedItem is ITabViewModel selectedTab && selectedTab != _lastSelectedTab)
        {
            _lastSelectedTab = selectedTab;
            _tabService.SetActiveTab(selectedTab);
        }
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
            UpdateTabStripSelection(activeTab);
            UpdateContentArea(activeTab);
        }
        finally
        {
            _isUpdatingActiveTab = false;
        }
    }

    private void UpdateTabStripSelection(ITabViewModel activeTab)
    {
        if (_tabStrip.SelectedItem != activeTab) _tabStrip.SelectedItem = activeTab;
    }

    private void UpdateContentArea(ITabViewModel activeTab)
    {
        if (activeTab != null)
        {
            var editorControl = _editorControlFactory.CreateEditorControl(activeTab.EditorViewModel);
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

    private void SaveCurrentTabScrollPosition(ITabViewModel previousTab)
    {
        if (_contentArea.Content is EditorControl editorControl && previousTab != null)
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

    private void OnThemeChanged(object sender, Theme e)
    {
        if (Content is Grid grid) grid.Background = _avaloniaConfig.BackgroundBrush;

        _tabStrip.Background = _avaloniaConfig.TabBackgroundBrush;
        _tabStrip.Foreground = _avaloniaConfig.TabForegroundBrush;

        // Refresh the content area to apply new theme
        if (_contentArea.Content is EditorControl editorControl) editorControl.InvalidateVisual();
    }
}