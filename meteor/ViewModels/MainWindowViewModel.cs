namespace meteor.ViewModels;

public class MainWindowViewModel(
    StatusPaneViewModel statusPaneViewModel,
    ScrollableTextEditorViewModel scrollableTextEditorViewModel)
    : ViewModelBase
{
    public StatusPaneViewModel StatusPaneViewModel { get; } = statusPaneViewModel;
    public ScrollableTextEditorViewModel ScrollableTextEditorViewModel { get; } = scrollableTextEditorViewModel;
}