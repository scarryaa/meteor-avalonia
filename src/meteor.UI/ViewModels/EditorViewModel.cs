using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Events;
using meteor.Core.Models.SyntaxHighlighting;

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
        get => TextBufferService.GetText();
        set
        {
            if (TextBufferService.GetText() != value)
            {
                TextBufferService.ReplaceAll(value);
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
        OnPropertyChanged(nameof(Text));
        UpdateHighlighting();
    }

    public void DeleteText(int index, int length)
    {
        _inputService.DeleteText(index, length);
        OnPropertyChanged(nameof(Text));
        UpdateHighlighting();
    }

    private void UpdateHighlighting()
    {
        var results = _syntaxHighlighter.Highlight(TextBufferService.GetText());
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