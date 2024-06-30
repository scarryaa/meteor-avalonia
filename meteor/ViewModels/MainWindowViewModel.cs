using meteor.Interfaces;

namespace meteor.ViewModels;

public class MainWindowViewModel(
    StatusPaneViewModel statusPaneViewModel,
    ScrollableTextEditorViewModel scrollableTextEditorViewModel,
    FontPropertiesViewModel fontPropertiesViewModel,
    LineCountViewModel lineCountViewModel,
    ICursorPositionService cursorPositionService)
    : ViewModelBase
{
    public StatusPaneViewModel StatusPaneViewModel { get; } = statusPaneViewModel;
    public ScrollableTextEditorViewModel ScrollableTextEditorViewModel { get; } = scrollableTextEditorViewModel;

    public GutterViewModel GutterViewModel { get; } = new(cursorPositionService, fontPropertiesViewModel,
        lineCountViewModel,
        scrollableTextEditorViewModel,
        scrollableTextEditorViewModel.TextEditorViewModel);
}