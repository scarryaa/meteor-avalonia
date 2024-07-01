using System.Windows.Input;
using ReactiveUI;

namespace meteor.ViewModels;

public class TabViewModel : ViewModelBase
{
    private string _title = "Untitled";
    private bool _isSelected;
    private ScrollableTextEditorViewModel _scrollableTextEditorViewModel;
    private string? _filePath;
    private bool _isDirty;
    private bool _isTemporary;
    private double _savedVerticalOffset;
    private double _savedHorizontalOffset;

    public ICommand CloseTabCommand { get; set; }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public bool IsTemporary
    {
        get => _isTemporary;
        set => this.RaiseAndSetIfChanged(ref _isTemporary, value);
    }

    public ScrollableTextEditorViewModel ScrollableTextEditorViewModel
    {
        get => _scrollableTextEditorViewModel;
        set => this.RaiseAndSetIfChanged(ref _scrollableTextEditorViewModel, value);
    }

    public string? FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    public double SavedVerticalOffset
    {
        get => _savedVerticalOffset;
        set => this.RaiseAndSetIfChanged(ref _savedVerticalOffset, value);
    }

    public double SavedHorizontalOffset
    {
        get => _savedHorizontalOffset;
        set => this.RaiseAndSetIfChanged(ref _savedHorizontalOffset, value);
    }
}