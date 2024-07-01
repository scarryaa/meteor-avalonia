using System;
using System.Globalization;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.Models;
using ReactiveUI;

namespace meteor.ViewModels;

public class TextEditorViewModel : ViewModelBase
{
    private readonly ICursorPositionService _cursorPositionService;
    private TextBuffer _textBuffer;
    private long _cursorPosition;
    private long _selectionStart = -1;
    private long _selectionEnd = -1;
    private bool _isSelecting;
    private double _windowHeight;
    private double _lineHeight;
    private double _windowWidth;
    private long _desiredColumn;
    private readonly LineCountViewModel _lineCountViewModel;

    public FontPropertiesViewModel FontPropertiesViewModel { get; }

    public TextEditorViewModel(ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel)
    {
        _cursorPositionService = cursorPositionService;
        FontPropertiesViewModel = fontPropertiesViewModel;
        _lineCountViewModel = lineCountViewModel;

        _textBuffer = new TextBuffer();
        _textBuffer.LinesUpdated += OnLinesUpdated;

        this.WhenAnyValue(x => x.FontPropertiesViewModel.FontFamily)
            .Subscribe(font => FontFamily = font);
        this.WhenAnyValue(x => x.FontPropertiesViewModel.FontSize)
            .Subscribe(size => FontSize = size);
        this.WhenAnyValue(x => x.FontPropertiesViewModel.LineHeight)
            .Subscribe(height => { LineHeight = height; });

        UpdateLineStarts();
    }

    public event EventHandler SelectionChanged;
    public event EventHandler LineChanged;
    public event EventHandler? InvalidateRequired;
    public event EventHandler? RequestFocus;

    public virtual void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    public void Focus()
    {
        RequestFocus?.Invoke(this, EventArgs.Empty);
    }
    
    public bool ShouldScrollToCursor { get; set; } = true;

    public LineCache LineCache { get; } = new();

    public FontFamily FontFamily
    {
        get => FontPropertiesViewModel.FontFamily;
        set => FontPropertiesViewModel.FontFamily = value;
    }

    public double FontSize
    {
        get => FontPropertiesViewModel.FontSize;
        set => FontPropertiesViewModel.FontSize = value;
    }

    public double LineHeight
    {
        get => FontPropertiesViewModel.LineHeight;
        set => this.RaiseAndSetIfChanged(ref _lineHeight, value);
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set
        {
            if (_windowHeight != value) this.RaiseAndSetIfChanged(ref _windowHeight, value);
        }
    }

    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (_windowWidth != value) this.RaiseAndSetIfChanged(ref _windowWidth, value);
        }
    }

    public long CursorPosition
    {
        get => _cursorPosition;
        set
        {
            if (_cursorPosition != value)
            {
                _cursorPosition = value;
                _cursorPositionService.UpdateCursorPosition(_cursorPosition, _textBuffer.LineStarts);
                NotifySelectionChanged();
                this.RaisePropertyChanged();
            }
        }
    }

    public long SelectionStart
    {
        get => _selectionStart;
        set
        {
            if (_selectionStart != value)
            {
                this.RaiseAndSetIfChanged(ref _selectionStart, value);
                NotifySelectionChanged();
            }
        }
    }

    public long SelectionEnd
    {
        get => _selectionEnd;
        set
        {
            if (_selectionEnd != value)
            {
                this.RaiseAndSetIfChanged(ref _selectionEnd, value);
                NotifySelectionChanged();
            }
        }
    }

    public bool IsSelecting
    {
        get => _isSelecting;
        set
        {
            if (_isSelecting != value) this.RaiseAndSetIfChanged(ref _isSelecting, value);
        }
    }

    private void NotifySelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public long LineCount => _textBuffer.LineCount;

    public double TotalHeight => _textBuffer.TotalHeight;

    public TextBuffer TextBuffer
    {
        get => _textBuffer;
        set
        {
            this.RaiseAndSetIfChanged(ref _textBuffer, value);
            this.RaisePropertyChanged(nameof(LineCount));
            this.RaisePropertyChanged(nameof(TotalHeight));
            _lineCountViewModel.LineCount = _textBuffer.LineCount;
        }
    }

    public void NotifyGutterOfLineChange()
    {
        LineChanged?.Invoke(this, EventArgs.Empty);
    }

    public void InsertText(long position, string text)
    {
        _textBuffer.InsertText(position, text);
        CursorPosition = position + text.Length;
        NotifyGutterOfLineChange();
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(TotalHeight));
    }

    public void DeleteText(long start, long length)
    {
        _textBuffer.DeleteText(start, length);
        NotifyGutterOfLineChange();
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(TotalHeight));
    }

    public void ClearSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }

    public void UpdateLineStarts()
    {
        _textBuffer.UpdateLineCache();
    }

    private void OnLinesUpdated(object sender, EventArgs e)
    {
        UpdateLineStarts();
        this.RaisePropertyChanged(nameof(TotalHeight));
    }

    public double CharWidth
    {
        get
        {
            var formattedText = new FormattedText(
                "x",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily),
                FontSize,
                Brushes.Black);
            return formattedText.Width;
        }
    }
}