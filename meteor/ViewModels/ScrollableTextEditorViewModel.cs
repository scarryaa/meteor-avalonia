using ReactiveUI;

namespace meteor.ViewModels;

public class ScrollableTextEditorViewModel : ViewModelBase
{
    private double _verticalOffset;
    private double _horizontalOffset;

    public ScrollableTextEditorViewModel()
    {
        TextEditorViewModel = new TextEditorViewModel();
    }

    public TextEditorViewModel TextEditorViewModel { get; }

    public double VerticalOffset
    {
        get => _verticalOffset;
        set
        {
            if (_verticalOffset != value) this.RaiseAndSetIfChanged(ref _verticalOffset, value);
        }
    }

    public double HorizontalOffset
    {
        get => _horizontalOffset;
        set
        {
            if (_horizontalOffset != value) this.RaiseAndSetIfChanged(ref _horizontalOffset, value);
        }
    }
}