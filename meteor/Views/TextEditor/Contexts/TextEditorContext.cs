using Avalonia.Media;
using meteor.ViewModels;

namespace meteor.Views.Contexts;

public class TextEditorContext
{
    public double LineHeight { get; set; }
    public IBrush BackgroundBrush { get; set; }
    public IBrush LineHighlightBrush { get; set; }
    public IBrush SelectionBrush { get; set; }
    public IBrush CursorBrush { get; set; }
    public double LinePadding { get; set; }
    public double SelectionEndPadding { get; set; }
    public FontFamily FontFamily { get; set; }
    public double FontSize { get; set; }
    public IBrush Foreground { get; set; }
    public ScrollableTextEditorViewModel ScrollableViewModel { get; set; }

    public TextEditorContext(
        double lineHeight,
        IBrush backgroundBrush,
        IBrush lineHighlightBrush,
        IBrush selectionBrush,
        IBrush cursorBrush,
        double linePadding,
        double selectionEndPadding,
        FontFamily fontFamily,
        double fontSize,
        IBrush foreground,
        ScrollableTextEditorViewModel scrollableViewModel)
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