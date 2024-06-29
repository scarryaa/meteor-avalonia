namespace meteor.ViewModels;

public class MainWindowViewModel(
    StatusPaneViewModel statusPaneViewModel,
    ScrollableTextEditorViewModel scrollableTextEditorViewModel,
    FontPropertiesViewModel fontPropertiesViewModel,
    LineCountViewModel lineCountViewModel)
    : ViewModelBase
{
    public StatusPaneViewModel StatusPaneViewModel { get; } = statusPaneViewModel;
    public ScrollableTextEditorViewModel ScrollableTextEditorViewModel { get; } = scrollableTextEditorViewModel;
    public GutterViewModel GutterViewModel { get; } = new(fontPropertiesViewModel, lineCountViewModel);
}