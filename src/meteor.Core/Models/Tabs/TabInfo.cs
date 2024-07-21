using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Models.Tabs;

public class TabInfo
{
    public int Index { get; }
    public string Title { get; }
    public ITextBufferService TextBufferService { get; }
    public IEditorViewModel EditorViewModel { get; }
    public int CursorPosition { get; set; }
    public (int start, int length) Selection { get; set; }
    public Vector ScrollOffset { get; set; }
    public double MaxScrollHeight { get; set; }

    public TabInfo(int index, string title, ITextBufferService textBufferService, IEditorViewModel editorViewModel)
    {
        Index = index;
        Title = title;
        TextBufferService = textBufferService;
        EditorViewModel = editorViewModel;
        CursorPosition = 0;
        Selection = (0, 0);
        ScrollOffset = new Vector(0, 0);
        MaxScrollHeight = 0;
    }
}