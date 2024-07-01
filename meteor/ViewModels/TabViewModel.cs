using System;
using System.IO;
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
    private string _originalText = string.Empty;
    private string _text = string.Empty;
    private bool _isLoadingText;

    public event EventHandler TextChanged;

    public ICommand CloseTabCommand { get; set; }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public bool IsNew { get; set; }

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

    public string OriginalText
    {
        get => _originalText;
        set => this.RaiseAndSetIfChanged(ref _originalText, value);
    }

    public ScrollableTextEditorViewModel ScrollableTextEditorViewModel
    {
        get => _scrollableTextEditorViewModel;
        set
        {
            if (_scrollableTextEditorViewModel != value)
            {
                if (_scrollableTextEditorViewModel != null)
                    _scrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.LinesUpdated -=
                        OnTextBufferLinesUpdated;

                this.RaiseAndSetIfChanged(ref _scrollableTextEditorViewModel, value);

                if (_scrollableTextEditorViewModel != null)
                    _scrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.LinesUpdated +=
                        OnTextBufferLinesUpdated;
            }
        }
    }

    public string? FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    public string Text
    {
        get => _text;
        set
        {
            if (value != _text)
            {
                this.RaiseAndSetIfChanged(ref _text, value);
                TextChanged?.Invoke(this, EventArgs.Empty);

                if (!_isLoadingText)
                {
                    IsDirty = value != OriginalText;
                    if (IsTemporary) IsTemporary = false;
                }
            }
        }
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

    public void LoadText(string filePath)
    {
        _isLoadingText = true;
        _originalText = File.ReadAllText(filePath);
        Text = _originalText;
        ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.Clear();
        ScrollableTextEditorViewModel.TextEditorViewModel.LineCache.Clear();
        ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.SetText(_originalText);
        ScrollableTextEditorViewModel.TextEditorViewModel.OnInvalidateRequired();
        ScrollableTextEditorViewModel.GutterViewModel.OnInvalidateRequired();
        _isLoadingText = false;
    }

    private void OnTextBufferLinesUpdated(object sender, EventArgs e)
    {
        Text = ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.Text;
    }
}
