using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Media;
using meteor.Interfaces;
using meteor.Models;
using ReactiveUI;

namespace meteor.ViewModels;

public class TextEditorViewModel : ViewModelBase
{
    private readonly ICursorPositionService _cursorPositionService;
    private ITextBuffer _textBuffer;
    private long _cursorPosition;
    private long _selectionStart = -1;
    private long _selectionEnd = -1;
    private bool _isSelecting;
    private double _windowHeight;
    private double _lineHeight;
    private double _windowWidth;
    private long _desiredColumn;
    private readonly LineCountViewModel _lineCountViewModel;
    private double _charWidth;
    private bool _charWidthNeedsUpdate = true;
    private double _fontSize;
    private FontFamily _fontFamily;
    private readonly IClipboardService _clipboardService;
    
    public FontPropertiesViewModel FontPropertiesViewModel { get; }

    public TextEditorViewModel(ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        ITextBuffer textBuffer,
        IClipboardService clipboardService)
    {
        _cursorPositionService = cursorPositionService;
        FontPropertiesViewModel = fontPropertiesViewModel;
        FontFamily = FontFamily.DefaultFontFamilyName;
        _lineCountViewModel = lineCountViewModel;
        _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));

        _textBuffer.TextChanged += OnLinesUpdated;

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
        get => _fontFamily;
        set
        {
            this.RaiseAndSetIfChanged(ref _fontFamily, value);
            _charWidthNeedsUpdate = true;
            this.RaisePropertyChanged(nameof(CharWidth));
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            this.RaiseAndSetIfChanged(ref _fontSize, value);
            _charWidthNeedsUpdate = true;
            this.RaisePropertyChanged(nameof(CharWidth));
        }
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

    public ITextBuffer TextBuffer
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

    public double CharWidth
    {
        get
        {
            if (_charWidthNeedsUpdate) UpdateCharWidth();
            return _charWidth;
        }
        set
        {
            if (_charWidth != value)
            {
                this.RaiseAndSetIfChanged(ref _charWidth, value);
                _charWidthNeedsUpdate = false;
            }
        }
    }

    public async Task CopyText()
    {
        if (SelectionStart != SelectionEnd)
        {
            var selectedText = _textBuffer.GetText(SelectionStart, SelectionEnd - SelectionStart);
            await _clipboardService.SetTextAsync(selectedText);
        }
    }

    public async Task PasteText()
    {
        var clipboardText = await _clipboardService.GetTextAsync();
        if (!string.IsNullOrEmpty(clipboardText)) InsertText(CursorPosition, clipboardText);
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
        OnInvalidateRequired();
    }

    public void DeleteText(long start, long length)
    {
        _textBuffer.DeleteText(start, length);
        NotifyGutterOfLineChange();
        this.RaisePropertyChanged(nameof(LineCount));
        this.RaisePropertyChanged(nameof(TotalHeight));
        OnInvalidateRequired();
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

    private void UpdateCharWidth()
    {
        try
        {
            var formattedText = new FormattedText(
                "x",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily),
                FontSize,
                Brushes.Black);
            _charWidth = formattedText.Width;
            _charWidthNeedsUpdate = false;
        }
        catch (Exception ex)
        {
            _charWidth = 10;
            _charWidthNeedsUpdate = false;
        }
    }
}
