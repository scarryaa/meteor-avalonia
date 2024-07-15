using meteor.Core.Interfaces.Rendering;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Interfaces.Contexts;

public interface ITextEditorContext
{
    IScrollableTextEditorViewModel ScrollableViewModel { get; set; }
    IBrush BackgroundBrush { get; }
    double LineHeight { get; }
}