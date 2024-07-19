namespace meteor.Core.Interfaces.Services;

public interface ITabService
{
    ITextBufferService GetActiveTextBufferService();
    void SwitchTab(int tabIndex);
    void RegisterTab(int tabIndex, ITextBufferService textBufferService);
    int GetNextAvailableTabIndex();
}