using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using meteor.Core.Models.Tabs;

namespace meteor.Core.Services;

public class TabService : ITabService, IDisposable
{
    private bool _disposed;
    private readonly Dictionary<int, TabInfo?> _tabs = new();
    private int _activeTabIndex = -1;
    private int _nextTabIndex = 1;

    public event EventHandler<TabChangedEventArgs> TabChanged;

    public ITextBufferService GetActiveTextBufferService()
    {
        if (_activeTabIndex == -1 || !_tabs.TryGetValue(_activeTabIndex, out var tab))
            return new TextBufferService();
        return tab.TextBufferService;
    }

    public void SwitchTab(int tabIndex)
    {
        if (_tabs.ContainsKey(tabIndex))
        {
            // Save current tab state
            if (_activeTabIndex != -1 && _tabs.TryGetValue(_activeTabIndex, out var currentTab))
                SaveTabState(currentTab);

            _activeTabIndex = tabIndex;

            // Restore new tab state
            if (_tabs.TryGetValue(_activeTabIndex, out var newTab))
                RestoreTabState(newTab);

            OnTabChanged(new TabChangedEventArgs(GetActiveTab()));
        }
        else
        {
            throw new ArgumentException("Tab does not exist.");
        }
    }

    public TabInfo AddTab(ITextBufferService textBufferService, IEditorViewModel editorViewModel)
    {
        var tabIndex = _nextTabIndex++;
        var newTab = new TabInfo(tabIndex, $"Tab {tabIndex}", textBufferService, editorViewModel);
        _tabs[tabIndex] = newTab;

        if (_activeTabIndex == -1)
            _activeTabIndex = tabIndex;

        OnTabChanged(new TabChangedEventArgs(newTab));

        return newTab;
    }

    public void CloseTab(int tabIndex)
    {
        if (_tabs.ContainsKey(tabIndex))
        {
            _tabs[tabIndex]?.EditorViewModel?.Dispose();
            _tabs.Remove(tabIndex);

            if (_activeTabIndex == tabIndex)
            {
                _activeTabIndex = _tabs.Keys.LastOrDefault(-1);
                OnTabChanged(new TabChangedEventArgs(GetActiveTab()));
            }
        }
    }

    public void CloseAllTabs()
    {
        foreach (var tab in _tabs.Values) tab?.EditorViewModel?.Dispose();

        _tabs.Clear();
        _activeTabIndex = -1;
        _nextTabIndex = 1;
        OnTabChanged(new TabChangedEventArgs(null));
    }

    public void CloseOtherTabs(int keepTabIndex)
    {
        foreach (var tab in _tabs.Values.Where(t => t?.Index != keepTabIndex)) tab?.EditorViewModel?.Dispose();

        if (_tabs.ContainsKey(keepTabIndex))
        {
            var tabToKeep = _tabs[keepTabIndex];
            _tabs.Clear();
            _tabs[keepTabIndex] = tabToKeep;
            _activeTabIndex = keepTabIndex;
            OnTabChanged(new TabChangedEventArgs(tabToKeep));
        }
    }

    public void SelectTab(int tabIndex)
    {
        if (_tabs.ContainsKey(tabIndex) && _activeTabIndex != tabIndex)
        {
            _activeTabIndex = tabIndex;
            OnTabChanged(new TabChangedEventArgs(GetActiveTab()));
        }
    }

    private void SaveTabState(TabInfo tab)
    {
        tab.CursorPosition = tab.EditorViewModel.CursorPosition;
        tab.Selection = tab.EditorViewModel.Selection;
        tab.ScrollOffset = tab.EditorViewModel.ScrollOffset;
        tab.MaxScrollHeight = tab.EditorViewModel.MaxScrollHeight;
    }

    private void RestoreTabState(TabInfo tab)
    {
        tab.EditorViewModel.SuppressNotifications(true);

        try
        {
            tab.EditorViewModel.MaxScrollHeight = tab.MaxScrollHeight;
            tab.EditorViewModel.CursorPosition = tab.CursorPosition;
            tab.EditorViewModel.Selection = tab.Selection;

            // Ensure scroll offset is within bounds
            var maxScrollY = Math.Min(tab.ScrollOffset.Y, tab.MaxScrollHeight);
            tab.EditorViewModel.RaiseInvalidateMeasure();
            tab.EditorViewModel.DispatcherInvoke(() =>
            {
                tab.EditorViewModel.UpdateScrollOffset(new Vector(tab.ScrollOffset.X, maxScrollY));
            });
        }
        finally
        {
            tab.EditorViewModel.SuppressNotifications(false);
        }
    }

    public void UpdateTabState(int tabIndex, int cursorPosition, (int start, int length) selection, Vector scrollOffset,
        double maxScrollHeight)
    {
        if (_tabs.TryGetValue(tabIndex, out var tab))
        {
            tab.CursorPosition = cursorPosition;
            tab.Selection = selection;
            tab.ScrollOffset = scrollOffset;
            tab.MaxScrollHeight = maxScrollHeight;
        }
    }

    public IEnumerable<TabInfo?> GetAllTabs()
    {
        return _tabs.Values;
    }

    public TabInfo? GetActiveTab()
    {
        return _activeTabIndex != -1 && _tabs.TryGetValue(_activeTabIndex, out var tab)
            ? tab
            : null;
    }

    protected virtual void OnTabChanged(TabChangedEventArgs e)
    {
        TabChanged?.Invoke(this, e);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                CloseAllTabs();
                TabChanged = null;
            }

            _disposed = true;
        }
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~TabService()
    {
        Dispose(false);
    }
}
