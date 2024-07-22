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
    private int _cachedCursorPosition = -1;
    private int _lastKnownCursorPosition = -1;
    private int _lastKnownLine = 1;
    private double _viewportHeight;
    private double _viewportWidth;
    private bool _suppressNotifications;
    private const double Epsilon = 0.000001;
    private readonly ICursorService _cursorService;
    private readonly IInputService _inputService;
    private readonly ISelectionService _selectionService;
    private readonly IEditorSizeCalculator _sizeCalculator;
    private readonly StringBuilder _stringBuilder = new();
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly IScrollManager _scrollManager;
    private string _cachedText = string.Empty;
    private double _editorHeight;
    private double _editorWidth;
    private bool _isTextDirty = true;
    private Vector _scrollOffset;
    private double _maxScrollHeight;
    private int _currentLine;
    private ObservableCollection<SyntaxHighlightingResult> _highlightingResults = new();

    public EditorViewModel(EditorViewModelServiceContainer serviceContainer, ITextMeasurer textMeasurer,
        IScrollManager scrollManager)
    {
        TextBufferService = serviceContainer.TextBufferService;
        TabService = serviceContainer.TabService;
        _syntaxHighlighter = serviceContainer.SyntaxHighlighter;
        _selectionService = serviceContainer.SelectionService;
        _inputService = serviceContainer.InputService;
        _cursorService = serviceContainer.CursorService;
        _sizeCalculator = serviceContainer.SizeCalculator;
        _scrollManager = scrollManager;

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
            if (_cachedCursorPosition == -1)
            {
                _cachedCursorPosition = _cursorService.GetCursorPosition();
                SynchronizeCurrentLine(_cachedCursorPosition);
            }

            return _cachedCursorPosition;
        }
        set
        {
            if (_cachedCursorPosition != value)
            {
                _cursorService.SetCursorPosition(value);
                _cachedCursorPosition = value;
                SynchronizeCurrentLine(value);
                UpdateTabState();
                OnPropertyChanged();
            }
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
        InvalidateCursorPosition();
        OnPropertyChanged(nameof(Text));
        UpdateHighlighting();
        UpdateLineCount();
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
        InvalidateCursorPosition();
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        _inputService.HandlePointerMoved(e);
        InvalidateCursorPosition();
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
    }

    public void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _inputService.HandlePointerReleased(e);
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(CursorPosition));
    }

    public async void OnTextInput(TextInputEventArgs e)
    {
        _inputService.HandleTextInput(e);
        _isTextDirty = true;
        InvalidateCursorPosition();
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Selection));
        UpdateHighlighting();
        UpdateEditorSize();
        UpdateLineCount();
        var offset = await _scrollManager.CalculateScrollOffsetAsync(CursorPosition, EditorWidth, EditorHeight,
            _viewportWidth,
            _viewportHeight, new Vector(ScrollOffset.X, ScrollOffset.Y), Text.Length);
        UpdateScrollOffset(offset);
    }

    public async Task OnKeyDown(KeyEventArgs e)
    {
        await _inputService.HandleKeyDown(e.Key, e.Modifiers);
        _isTextDirty = true;
        InvalidateCursorPosition();
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Selection));
        SynchronizeCurrentLine(_cursorService.GetCursorPosition());
        OnPropertyChanged(nameof(CursorPosition));
        UpdateHighlighting();
        UpdateEditorSize();
        UpdateLineCount();
        var offset = await _scrollManager.CalculateScrollOffsetAsync(CursorPosition, EditorWidth, EditorHeight,
            _viewportWidth,
            _viewportHeight, new Vector(ScrollOffset.X, ScrollOffset.Y), Text.Length);
        UpdateScrollOffset(offset);
    }

    private void SynchronizeCurrentLine(int cursorPosition)
    {
        if (cursorPosition < 0 || cursorPosition > TextBufferService.Length)
        {
            // Invalid cursor position, reset to known good state
            _lastKnownCursorPosition = 0;
            _lastKnownLine = 1;
            CurrentLine = 1;
            return;
        }

        if (cursorPosition == _lastKnownCursorPosition)
        {
            CurrentLine = _lastKnownLine;
            return;
        }

        int newLine;

        if (cursorPosition > _lastKnownCursorPosition)
            // Moving forward, start counting from last known position
            newLine = _lastKnownLine + CountNewlinesBetween(_lastKnownCursorPosition, cursorPosition);
        else
            // Moving backward, start counting from the beginning
            newLine = 1 + CountNewlinesBetween(0, cursorPosition);

        CurrentLine = newLine;
        _lastKnownCursorPosition = cursorPosition;
        _lastKnownLine = newLine;
    }

    private int CountNewlinesBetween(int start, int end)
    {
        var count = 0;
        var length = TextBufferService.Length;

        // Ensure start and end are within bounds
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Max(0, Math.Min(end, length));

        for (var i = start; i < end; i++)
            if (TextBufferService[i] == '\n')
                count++;
        return count;
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

    public void UpdateViewportSize(double width, double height)
    {
        if (Math.Abs(_viewportWidth - width) > Epsilon || Math.Abs(_viewportHeight - height) > Epsilon)
        {
            _viewportWidth = width;
            _viewportHeight = height;
            UpdateScrollOffset(ScrollOffset);
        }
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

    public void UpdateEditorSize(double width, double height, double viewportHeight, double viewportWidth)
    {
        EditorWidth = width;
        EditorHeight = height;
        MaxScrollHeight = Math.Max(0, EditorHeight - viewportHeight);
        UpdateViewportSize(viewportWidth, viewportHeight);
        UpdateTabState();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (!_suppressNotifications) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void InvalidateCursorPosition()
    {
        _cachedCursorPosition = -1;
        OnPropertyChanged(nameof(CursorPosition));
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
