using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Contexts;
using meteor.Core.Interfaces.Rendering;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Rendering;

namespace meteor.Core.Contexts;

public class TextEditorContext(
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
    ITextEditorViewModel viewModel,
    FontStyle fontStyle,
    FontWeight fontWeight,
    IBrush foregroundBrush,
    double verticalOffset)
    : ITextEditorContext
{
    public double LineHeight { get; set; } = lineHeight;
    public IBrush BackgroundBrush { get; set; } = backgroundBrush;
    public IBrush LineHighlightBrush { get; set; } = lineHighlightBrush;
    public IBrush SelectionBrush { get; set; } = selectionBrush;
    public IBrush CursorBrush { get; set; } = cursorBrush;
    public double LinePadding { get; set; } = linePadding;
    public double SelectionEndPadding { get; set; } = selectionEndPadding;
    public IFontFamily FontFamily { get; set; } = fontFamily;
    public double FontSize { get; set; } = fontSize;
    public IBrush Foreground { get; set; } = foreground;
    public ITextEditorViewModel TextEditorViewModel { get; set; } = viewModel;
    public FontStyle FontStyle { get; set; } = fontStyle;
    public FontWeight FontWeight { get; set; } = fontWeight;
    public IBrush ForegroundBrush { get; set; } = foregroundBrush;
    public double VerticalOffset { get; set; }
}