using meteor.Core.Models.Tabs;

namespace meteor.Core.Interfaces.Services;

public interface ITabService
{
    ITextBufferService GetActiveTextBufferService();
    void SwitchTab(int tabIndex);
    TabInfo AddTab(ITextBufferService textBufferService);
    void CloseTab(int tabIndex);
    void CloseAllTabs();
    void CloseOtherTabs(int keepTabIndex);
    IEnumerable<TabInfo> GetAllTabs();
    TabInfo? GetActiveTab();
}