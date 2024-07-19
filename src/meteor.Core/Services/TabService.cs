using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class TabService : ITabService
{
    private readonly Dictionary<int, ITextBufferService> _tabs = new();
    private int _activeTabIndex = -1;
    private int _nextTabIndex = 1;

    public ITextBufferService GetActiveTextBufferService()
    {
        if (_activeTabIndex == -1 || !_tabs.TryGetValue(_activeTabIndex, out var textBufferService))
            // Return an empty TextBufferService if there's no active tab
            return new TextBufferService();
        return textBufferService;
    }

    public void SwitchTab(int tabIndex)
    {
        if (_tabs.ContainsKey(tabIndex))
            _activeTabIndex = tabIndex;
        else
            throw new ArgumentException("Tab does not exist.");
    }

    public void RegisterTab(int tabIndex, ITextBufferService textBufferService)
    {
        if (textBufferService == null && _tabs.ContainsKey(tabIndex))
            _tabs.Remove(tabIndex);
        else
            _tabs[tabIndex] = textBufferService;

        // If this is the first tab, make it active
        if (_activeTabIndex == -1)
            _activeTabIndex = tabIndex;

        // Update _nextTabIndex if necessary
        if (tabIndex >= _nextTabIndex)
            _nextTabIndex = tabIndex + 1;
    }

    public int GetNextAvailableTabIndex()
    {
        return _nextTabIndex++;
    }
}