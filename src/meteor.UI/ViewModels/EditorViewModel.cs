using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using meteor.Core.Models.SyntaxHighlighting;
using meteor.Core.Services;

namespace meteor.UI.ViewModels;

public sealed class EditorViewModel : IEditorViewModel
{
    private bool _suppressNotifications;
    private const double Epsilon = 0.000001;
    private readonly ICursorService _cursorService;
    private readonly IInputService _inputService;
    private readonly ISelectionService _selectionService;
    private readonly IEditorSizeCalculator _sizeCalculator;
    private readonly StringBuilder _stringBuilder = new();
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private string _cachedText = string.Empty;
    private double _editorHeight;
    private double _editorWidth;
    private bool _isTextDirty = true;
    private Vector _scrollOffset;
    private double _maxScrollHeight;
    private int _currentLine;
    private ObservableCollection<SyntaxHighlightingResult> _highlightingResults = new();

    public EditorViewModel(EditorViewModelServiceContainer serviceContainer, ITextMeasurer textMeasurer)
    {
        TextBufferService = serviceContainer.TextBufferService;
        TabService = serviceContainer.TabService;
        _syntaxHighlighter = serviceContainer.SyntaxHighlighter;
        _selectionService = serviceContainer.SelectionService;
        _inputService = serviceContainer.InputService;
        _cursorService = serviceContainer.CursorService;
        _sizeCalculator = serviceContainer.SizeCalculator;

        GutterViewModel = new GutterViewModel(textMeasurer)
        {
            LineCount = 1
        };
        GutterViewModel.ScrollOffsetChanged += OnGutterScrollOffsetChanged;
        UpdateLineCount();

        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ScrollOffset))
                GutterViewModel.ScrollOffset = ScrollOffset.Y;
        };

        TabService.TabChanged += OnTabChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public GutterViewModel GutterViewModel { get; }

    public int CurrentLine
    {
        get => _currentLine;
        set
        {
            if (_currentLine != value)
            {
                _currentLine = value;
                OnPropertyChanged();
                UpdateGutterCurrentLine();
            }
        }
    }
    
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
                GutterViewModel.ScrollOffset = value.Y;
                UpdateTabState();
            }
        }
    }

    public (int start, int length) Selection
    {
        get => _selectionService.GetSelection();
        set
        {
            _selectionService.SetSelection(value.start, value.length);
            UpdateTabState();
        }
    }

    public ITabService TabService { get; }
    public ITextBufferService TextBufferService { get; }

    public int CursorPosition
    {
        get
        {
            var position = _cursorService.GetCursorPosition();
            SynchronizeCurrentLine(position);
            return position;
        }
        set
        {
            _cursorService.SetCursorPosition(value);
            SynchronizeCurrentLine(value);
            UpdateTabState();
        }
    }

    public double EditorWidth
    {
        get => _editorWidth;
        private set
        {
            if (Math.Abs(_editorWidth - value) > Epsilon)
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
            if (Math.Abs(_editorHeight - value) > Epsilon)
            {
                _editorHeight = value;
                OnPropertyChanged();
            }
        }
    }

    public double MaxScrollHeight
    {
        get => _maxScrollHeight;
        set
        {
            if (Math.Abs(_maxScrollHeight - value) > Epsilon)
            {
                _maxScrollHeight = value;
                OnPropertyChanged();
            }
        }
    }

    public event EventHandler? InvalidateMeasureRequested;

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
                UpdateLineCount();
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
                UpdateLineCount();
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
        GutterViewModel.ScrollOffset = offset.Y;
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
        UpdateLineCount();
        CursorPosition = Math.Max(0, index - length);
    }

    public void RaiseInvalidateMeasure()
    {
        InvalidateMeasureRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateGutterCurrentLine()
    {
        GutterViewModel.CurrentLine = CurrentLine;
    }

    public void DispatcherInvoke(Action action)
    {
        Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
    }

    public void OnPointerPressed(PointerPressedEventArgs e)
    {
        _inputService.HandlePointerPressed(e);
        CursorPosition = _cursorService.GetCursorPosition();
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        _inputService.HandlePointerMoved(e);
        CursorPosition = _cursorService.GetCursorPosition();
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
        _isTextDirty = true;
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Selection));
        SynchronizeCurrentLine(_cursorService.GetCursorPosition());
        OnPropertyChanged(nameof(CursorPosition));
        UpdateHighlighting();
        UpdateEditorSize();
        UpdateLineCount();
    }

    public async Task OnKeyDown(KeyEventArgs e)
    {
        await _inputService.HandleKeyDown(e.Key, e.Modifiers);
        _isTextDirty = true;
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Selection));
        SynchronizeCurrentLine(_cursorService.GetCursorPosition());
        OnPropertyChanged(nameof(CursorPosition));
        UpdateHighlighting();
        UpdateEditorSize();
        UpdateLineCount();
    }

    private void SynchronizeCurrentLine(int cursorPosition)
    {
        CurrentLine = TextBufferService.GetLineNumberFromPosition(cursorPosition);
    }
    
    private void OnTabChanged(object? sender, TabChangedEventArgs e)
    {
        UpdateLineCount();
        OnPropertyChanged(nameof(Text));
    }

    private void OnGutterScrollOffsetChanged(object? sender, double newOffset)
    {
        ScrollOffset = new Vector(ScrollOffset.X, newOffset);
    }

    private void UpdateLineCount()
    {
        var textBufferService = TabService.GetActiveTextBufferService();
        var lineCount = 1;
        var index = 0;

        while (index < textBufferService.Length)
        {
            var newLineIndex = textBufferService.IndexOf('\n', index);
            if (newLineIndex == -1)
                break;

            lineCount++;
            index = newLineIndex + 1;
        }

        GutterViewModel.LineCount = lineCount;
    }

    private void UpdateEditorSize()
    {
        var textBufferService = TabService.GetActiveTextBufferService();
        var (width, height) = _sizeCalculator.CalculateEditorSize(textBufferService, EditorWidth, EditorHeight);
        EditorWidth = width;
        EditorHeight = height;
        MaxScrollHeight = Math.Max(0, EditorHeight);
    }

    private void UpdateHighlighting()
    {
        var results = _syntaxHighlighter.Highlight(Text);
        HighlightingResults = new ObservableCollection<SyntaxHighlightingResult>(results);
    }

    private void UpdateTabState()
    {
        if (_suppressNotifications) return;

        var activeTabInfo = TabService.GetActiveTab();
        if (activeTabInfo != null)
            TabService.UpdateTabState(activeTabInfo.Index, CursorPosition, Selection, ScrollOffset, MaxScrollHeight);
    }

    public void UpdateEditorSize(double width, double height, double viewportHeight)
    {
        EditorWidth = width;
        EditorHeight = height;
        MaxScrollHeight = Math.Max(0, EditorHeight - viewportHeight);
        UpdateTabState();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (!_suppressNotifications) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SuppressNotifications(bool suppress)
    {
        _suppressNotifications = suppress;
    }

    public void Dispose()
    {
        TabService.TabChanged -= OnTabChanged;
        GutterViewModel.ScrollOffsetChanged -= OnGutterScrollOffsetChanged;
        PropertyChanged = null;
    }
}
