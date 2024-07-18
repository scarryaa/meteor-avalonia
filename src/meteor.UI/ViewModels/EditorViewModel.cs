using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Events;
using meteor.Core.Models.SyntaxHighlighting;
using meteor.Core.Services;

namespace meteor.UI.ViewModels;

public class EditorViewModel : IEditorViewModel
{
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly ISelectionService _selectionService;
    private readonly IInputService _inputService;
    private readonly ICursorService _cursorService;
    private readonly IEditorSizeCalculator _sizeCalculator;
    private ObservableCollection<SyntaxHighlightingResult> _highlightingResults = new();
    private double _editorWidth;
    private double _editorHeight;
    private readonly StringBuilder _stringBuilder = new();
    private string _cachedText;
    private bool _isTextDirty = true;
    private double _verticalScrollOffset;
    private double _horizontalScrollOffset;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public EditorViewModel(
        ITextBufferService textBufferService,
        ISyntaxHighlighter syntaxHighlighter,
        ISelectionService selectionService,
        IInputService inputService,
        ICursorService cursorService,
        IEditorSizeCalculator sizeCalculator)
    {
        TextBufferService = textBufferService;
        _syntaxHighlighter = syntaxHighlighter;
        _selectionService = selectionService;
        _inputService = inputService;
        _cursorService = cursorService;
        _sizeCalculator = sizeCalculator;
    }

    public (int start, int length) Selection => _selectionService.GetSelection();
    public ITextBufferService TextBufferService { get; }

    public int CursorPosition => _cursorService.GetCursorPosition();

    public double EditorWidth
    {
        get => _editorWidth;
        private set
        {
            if (_editorWidth != value)
            {
                _editorWidth = value;
                OnPropertyChanged();
            }
        }
    }

    public double EditorHeight
    {
        get => _editorHeight;
        private set
        {
            if (_editorHeight != value)
            {
                _editorHeight = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string Text
    {
        get
        {
            if (_isTextDirty)
            {
                _stringBuilder.Clear();
                TextBufferService.AppendTo(_stringBuilder);
                _cachedText = _stringBuilder.ToString();
                _isTextDirty = false;
            }

            return _cachedText;
        }
        set
        {
            if (_cachedText != value)
            {
                TextBufferService.ReplaceAll(value);
                _cachedText = value;
                _isTextDirty = false;
                OnPropertyChanged();
                UpdateHighlighting();
            }
        }
    }
    public ObservableCollection<SyntaxHighlightingResult> HighlightingResults
    {
        get => _highlightingResults;
        private set
        {
            if (_highlightingResults != value)
            {
                _highlightingResults = value;
                OnPropertyChanged();
            }
        }
    }

    public void UpdateScrollOffset(double horizontalScrollOffset, double verticalScrollOffset)
    {
        _verticalScrollOffset = verticalScrollOffset;
        _horizontalScrollOffset = horizontalScrollOffset;
        (_inputService as InputService)?.UpdateScrollOffset(verticalScrollOffset, horizontalScrollOffset);
    }
    
    public void UpdateWindowSize(double width, double height)
    {
        _sizeCalculator.UpdateWindowSize(width, height);
        UpdateEditorSize();
    }

    private void UpdateEditorSize()
    {
        var (width, height) = _sizeCalculator.CalculateEditorSize(TextBufferService, EditorWidth, EditorHeight);
        EditorWidth = width;
        EditorHeight = height;
    }

    public void InsertText(int index, string text)
    {
        _inputService.InsertText(text);
        _isTextDirty = true;
        OnPropertyChanged(nameof(Text));
        UpdateHighlighting();
    }


    public void DeleteText(int index, int length)
    {
        _inputService.DeleteText(index, length);
        _isTextDirty = true;
        OnPropertyChanged(nameof(Text));
        UpdateHighlighting();
    }

    private void UpdateHighlighting()
    {
        var results = _syntaxHighlighter.Highlight(Text);
        HighlightingResults = new ObservableCollection<SyntaxHighlightingResult>(results);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void OnPointerPressed(PointerPressedEventArgs e)
    {
        _inputService.HandlePointerPressed(e);
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
    }
    
    public void OnPointerMoved(PointerEventArgs e)
    {
        _inputService.HandlePointerMoved(e);
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
    }

    public void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _inputService.HandlePointerReleased(e);
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
    }

    public void OnTextInput(TextInputEventArgs e)
    {
        _inputService.HandleTextInput(e);
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
        UpdateHighlighting();
        UpdateEditorSize();
    }

    public async Task OnKeyDown(KeyEventArgs e)
    {
        await _inputService.HandleKeyDown(e.Key, e.Modifiers);
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
        UpdateHighlighting();
        UpdateEditorSize();
    }
}
