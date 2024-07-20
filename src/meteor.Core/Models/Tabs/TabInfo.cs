using meteor.Core.Interfaces.Services;

namespace meteor.Core.Models.Tabs;

public class TabInfo
{
    public int Index { get; }
    public string Title { get; }
    public ITextBufferService TextBufferService { get; }

    public TabInfo(int index, string title, ITextBufferService textBufferService)
    {
        Index = index;
        Title = title;
        TextBufferService = textBufferService;
    }
}