using meteor.Core.Models.Tabs;

namespace meteor.Core.Models.Events;

public class TabChangedEventArgs : EventArgs
{
    public TabInfo? NewTab { get; }

    public TabChangedEventArgs(TabInfo? newTab)
    {
        NewTab = newTab;
    }
}