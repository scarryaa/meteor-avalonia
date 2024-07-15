using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Contexts;

public class TextEditorContext : ITextEditorContext
{
    public double LineHeight { get; set; }
    public IBrush BackgroundBrush { get; set; }
    public IBrush LineHighlightBrush { get; set; }
    public IBrush SelectionBrush { get; set; }
    public IBrush CursorBrush { get; set; }
    public double LinePadding { get; set; }
    public double SelectionEndPadding { get; set; }
    public IFontFamily FontFamily { get; set; }
    public double FontSize { get; set; }
    public IBrush Foreground { get; set; }
    public IScrollableTextEditorViewModel ScrollableViewModel { get; set; }

    public TextEditorContext(
        double lineHeight,
        IBrush backgroundBrush,
        IBrush lineHighlightBrush,
        IBrush selectionBrush,
        IBrush cursorBrush,
        double linePadding,
        double selectionEndPadding,
        IFontFamily fontFamily,
        double fontSize,
        IBrush foreground,
        IScrollableTextEditorViewModel scrollableViewModel)
    {
        LineHeight = lineHeight;
        BackgroundBrush = backgroundBrush;
        LineHighlightBrush = lineHighlightBrush;
        SelectionBrush = selectionBrush;
        CursorBrush = cursorBrush;
        LinePadding = linePadding;
        SelectionEndPadding = selectionEndPadding;
        FontFamily = fontFamily;
        FontSize = fontSize;
        Foreground = foreground;
        ScrollableViewModel = scrollableViewModel;
    }
}