using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using meteor.Core.Models.Tabs;

namespace meteor.Core.Interfaces.Services;

public interface ITabService
{
    event EventHandler<TabChangedEventArgs> TabChanged;
    
    ITextBufferService GetActiveTextBufferService();
    void SwitchTab(int tabIndex);
    TabInfo? AddTab(ITextBufferService textBufferService, IEditorViewModel editorViewModel);
    void CloseTab(int tabIndex);
    void SelectTab(int tabIndex);
    void CloseAllTabs();
    void CloseOtherTabs(int keepTabIndex);
    void UpdateTabState(int tabIndex, int cursorPosition, (int start, int length) selection, Vector scrollOffset);
    IEnumerable<TabInfo?> GetAllTabs();
    TabInfo? GetActiveTab();
}