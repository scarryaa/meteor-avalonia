using meteor.Core.Interfaces.Rendering;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Rendering;

namespace meteor.Core.Interfaces.Contexts;

public interface ITextEditorContext
{
    ITextEditorViewModel TextEditorViewModel { get; set; }  
    IBrush BackgroundBrush { get; }
    double LineHeight { get; }
    IBrush LineHighlightBrush { get; }
    IBrush SelectionBrush { get; }
    IBrush CursorBrush { get; }
    double LinePadding { get; }
    double SelectionEndPadding { get; }
    IFontFamily FontFamily { get; }
    double FontSize { get; }
    IBrush Foreground { get; }
    FontStyle FontStyle { get; }
    FontWeight FontWeight { get; }
    IBrush ForegroundBrush { get; }
    double VerticalOffset { get; }  
}