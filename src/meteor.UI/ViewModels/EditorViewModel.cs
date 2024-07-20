using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using meteor.Core.Models.SyntaxHighlighting;
using meteor.Core.Services;

namespace meteor.UI.ViewModels;

public sealed class EditorViewModel : IEditorViewModel
{
    private readonly ICursorService _cursorService;
    private readonly IInputService _inputService;
    private readonly ISelectionService _selectionService;
    private readonly IEditorSizeCalculator _sizeCalculator;
    private readonly StringBuilder _stringBuilder = new();
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private string _cachedText;
    private double _editorHeight;
    private double _editorWidth;
    private ObservableCollection<SyntaxHighlightingResult> _highlightingResults = new();
    private double _horizontalScrollOffset;
    private bool _isTextDirty = true;
    private Vector _scrollOffset;
    private double _verticalScrollOffset;

    public EditorViewModel(
        ITextBufferService textBufferService,
        ITabService tabService,
        ISyntaxHighlighter syntaxHighlighter,
        ISelectionService selectionService,
        IInputService inputService,
        ICursorService cursorService,
        IEditorSizeCalculator sizeCalculator)
    {
        TextBufferService = textBufferService;
        TabService = tabService;
        _syntaxHighlighter = syntaxHighlighter;
        _selectionService = selectionService;
        _inputService = inputService;
        _cursorService = cursorService;
        _sizeCalculator = sizeCalculator;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Vector ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            if (_scrollOffset != value)
            {
                _scrollOffset = value;
                OnPropertyChanged();
                UpdateScrollOffset(_scrollOffset);
            }
        }
    }

    public (int start, int length) Selection => _selectionService.GetSelection();
    public ITabService TabService { get; }
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
                var textBufferService = TabService.GetActiveTextBufferService();
                _stringBuilder.Clear();
                textBufferService.AppendTo(_stringBuilder);
                _cachedText = _stringBuilder.ToString();
                _isTextDirty = false;
            }

            return _cachedText;
        }
        set
        {
            if (_cachedText != value)
            {
                var textBufferService = TabService.GetActiveTextBufferService();
                textBufferService.ReplaceAll(value);
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

    public void UpdateScrollOffset(Vector offset)
    {
        ScrollOffset = offset;
        (_inputService as InputService)?.UpdateScrollOffset(offset.Y, offset.X);
    }

    public void UpdateWindowSize(double width, double height)
    {
        _sizeCalculator.UpdateWindowSize(width, height);
        UpdateEditorSize();
    }
    
    public void DeleteText(int index, int length)
    {
        _inputService.DeleteText(index, length);
        _isTextDirty = true;
        OnPropertyChanged(nameof(Text));
        UpdateHighlighting();
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
        _isTextDirty = true;
    }

    public async Task OnKeyDown(KeyEventArgs e)
    {
        await _inputService.HandleKeyDown(e.Key, e.Modifiers);
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
        UpdateHighlighting();
        UpdateEditorSize();
        _isTextDirty = true;
    }

    private void UpdateEditorSize()
    {
        var textBufferService = TabService.GetActiveTextBufferService();
        var (width, height) = _sizeCalculator.CalculateEditorSize(textBufferService, EditorWidth, EditorHeight);
        EditorWidth = width;
        EditorHeight = height;
    }

    private void UpdateHighlighting()
    {
        Console.WriteLine("Text: " + Text);
        var results = _syntaxHighlighter.Highlight(Text);
        HighlightingResults = new ObservableCollection<SyntaxHighlightingResult>(results);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}