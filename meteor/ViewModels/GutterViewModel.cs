using System;
using Avalonia.Media;
using meteor.Interfaces;
using ReactiveUI;

namespace meteor.ViewModels;

public class GutterViewModel : ViewModelBase
{
    private readonly FontPropertiesViewModel _fontPropertiesViewModel;
    private double _lineHeight;
    private TextEditorViewModel _textEditorViewModel;
    private ScrollableTextEditorViewModel _scrollableTextEditorViewModel;

    public LineCountViewModel LineCountViewModel { get; }

    public TextEditorViewModel TextEditorViewModel
    {
        get => _textEditorViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _textEditorViewModel, value);
            if (_textEditorViewModel != null && InvalidateRequired != null)
                _textEditorViewModel.WhenAnyValue(te => te.CursorPosition)
                    .Subscribe(cursorPosition =>
                    {
                        CursorPosition = cursorPosition;
                        InvalidateRequired.Invoke(this, EventArgs.Empty);
                    });
        }
    }

    public ScrollableTextEditorViewModel ScrollableTextEditorViewModel
    {
        get => _scrollableTextEditorViewModel;
        set => this.RaiseAndSetIfChanged(ref _scrollableTextEditorViewModel, value);
    }

    public GutterViewModel(
        ICursorPositionService cursorPositionService,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel, ScrollableTextEditorViewModel scrollableTextEditorViewModel,
        TextEditorViewModel textEditorViewModel)
    {
        _fontPropertiesViewModel = fontPropertiesViewModel;
        LineCountViewModel = lineCountViewModel;
        TextEditorViewModel = textEditorViewModel;
        _scrollableTextEditorViewModel = scrollableTextEditorViewModel;

        cursorPositionService.CursorPositionChanged += (cursorPosition, _) =>
        {
            CursorPosition = cursorPosition;
            OnInvalidateRequired();
        };
        
        TextEditorViewModel.SelectionChanged += OnTextEditorSelectionChanged;
        
        _fontPropertiesViewModel.WhenAnyValue(x => x.LineHeight)
            .Subscribe(lineHeight => LineHeight = lineHeight);

        _fontPropertiesViewModel.WhenAnyValue(x => x.FontFamily)
            .Subscribe(fontFamily => FontFamily = fontFamily);

        _fontPropertiesViewModel.WhenAnyValue(x => x.FontSize)
            .Subscribe(fontSize => FontSize = fontSize);

        _lineHeight = _fontPropertiesViewModel.LineHeight;

        // Subscribe to VerticalOffset changes
        LineCountViewModel.WhenAnyValue(x => x.VerticalOffset)
            .Subscribe(offset => OnInvalidateRequired());

        // Subscribe to ViewportHeight changes
        LineCountViewModel.WhenAnyValue(x => x.ViewportHeight)
            .Subscribe(height => OnInvalidateRequired());

        // Subscribe to LineCount changes
        LineCountViewModel.WhenAnyValue(x => x.LineCount)
            .Subscribe(count => OnInvalidateRequired());

        TextEditorViewModel.LineChanged += (sender, args) =>
        {
            CursorPosition = TextEditorViewModel.CursorPosition;
            OnInvalidateRequired();
        };
    }

    private void OnTextEditorSelectionChanged(object sender, EventArgs e)
    {
        OnInvalidateRequired();
    }
    
    public event EventHandler? InvalidateRequired;

    public long CursorPosition { get; set; }

    public FontFamily FontFamily
    {
        get => _fontPropertiesViewModel.FontFamily;
        set
        {
            if (_fontPropertiesViewModel.FontFamily != value)
            {
                _fontPropertiesViewModel.FontFamily = value;
                OnInvalidateRequired();
            }
        }
    }

    public double FontSize
    {
        get => _fontPropertiesViewModel.FontSize;
        set
        {
            if (_fontPropertiesViewModel.FontSize != value)
            {
                _fontPropertiesViewModel.FontSize = value;
                OnInvalidateRequired();
            }
        }
    }

    public double LineHeight
    {
        get => _lineHeight;
        set
        {
            if (_lineHeight != value)
            {
                _lineHeight = value;
                _fontPropertiesViewModel.LineHeight = value;
                this.RaiseAndSetIfChanged(ref _lineHeight, value);
            }
        }
    }

    public double VerticalOffset
    {
        get => LineCountViewModel.VerticalOffset;
        set => LineCountViewModel.VerticalOffset = value;
    }

    public double ViewportHeight
    {
        get => LineCountViewModel.ViewportHeight;
        set => LineCountViewModel.ViewportHeight = value;
    }

    public long LineCount
    {
        get => LineCountViewModel.LineCount;
        set => LineCountViewModel.LineCount = value;
    }

    public virtual void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }
}