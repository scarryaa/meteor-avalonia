using Avalonia.Media;
using meteor.ViewModels;

namespace meteor.Views.Contexts;

public class TextEditorContext(
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
    public double LineHeight { get; set; } = lineHeight;
    public IBrush BackgroundBrush { get; set; } = backgroundBrush;
    public IBrush LineHighlightBrush { get; set; } = lineHighlightBrush;
    public IBrush SelectionBrush { get; set; } = selectionBrush;
    public IBrush CursorBrush { get; set; } = cursorBrush;
    public double LinePadding { get; set; } = linePadding;
    public double SelectionEndPadding { get; set; } = selectionEndPadding;
    public FontFamily FontFamily { get; set; } = fontFamily;
    public double FontSize { get; set; } = fontSize;
    public IBrush Foreground { get; set; } = foreground;
    public ScrollableTextEditorViewModel ScrollableViewModel { get; set; } = scrollableViewModel;
}