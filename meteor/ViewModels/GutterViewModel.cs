using System;
using System.Numerics;
using Avalonia.Media;
using ReactiveUI;

namespace meteor.ViewModels;

public class GutterViewModel : ViewModelBase
{
    private readonly FontPropertiesViewModel _fontPropertiesViewModel;
    private double _lineHeight;

    public LineCountViewModel LineCountViewModel { get; }

    public GutterViewModel(
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel)
    {
        _fontPropertiesViewModel = fontPropertiesViewModel;
        LineCountViewModel = lineCountViewModel;

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
    }

    public event EventHandler? InvalidateRequired;

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

    public BigInteger LineCount
    {
        get => LineCountViewModel.LineCount;
        set => LineCountViewModel.LineCount = value;
    }

    protected virtual void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }
}