using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.UI.ViewModels;

public class GutterViewModel : IGutterViewModel
{
    private readonly ITextMeasurer _textMeasurer;
    private int _lineCount = 1;
    private double _lineHeight;
    private double _scrollOffset;
    private int _currentLine = 1;
    private double _gutterWidth;
    private int _lastMeasuredDigitCount = 1;
    private double _viewportHeight;
    private int _visibleLineCount;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<double>? ScrollOffsetChanged;

    public GutterViewModel(ITextMeasurer textMeasurer)
    {
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        UpdateLineHeight();
        UpdateGutterWidth();
    }

    public int VisibleLineCount
    {
        get => _visibleLineCount;
        set => SetProperty(ref _visibleLineCount, value);
    }

    public double TotalHeight { get; private set; }

    public double ViewportHeight
    {
        get => _viewportHeight;
        set
        {
            if (SetProperty(ref _viewportHeight, value)) UpdateScrollOffset(_scrollOffset);
        }
    }

    public int LineCount
    {
        get => _lineCount;
        set
        {
            if (SetProperty(ref _lineCount, value))
            {
                UpdateGutterWidth();
                UpdateTotalHeight();
            }
        }
    }

    public double LineHeight
    {
        get => _lineHeight;
        private set
        {
            if (SetProperty(ref _lineHeight, value)) UpdateTotalHeight();
        }
    }

    public double ScrollOffset
    {
        get => _scrollOffset;
        set => UpdateScrollOffset(value);
    }

    public int CurrentLine
    {
        get => _currentLine;
        set => SetProperty(ref _currentLine, value);
    }

    public double GutterWidth
    {
        get => _gutterWidth;
        private set => SetProperty(ref _gutterWidth, value);
    }

    public void UpdateScrollOffset(double newOffset)
    {
        var maxScrollOffset = Math.Max(0, TotalHeight - ViewportHeight);
        newOffset = Math.Clamp(newOffset, 0, maxScrollOffset);

        if (SetProperty(ref _scrollOffset, newOffset, nameof(ScrollOffset)))
            ScrollOffsetChanged?.Invoke(this, newOffset);
    }

    private void UpdateLineHeight()
    {
        LineHeight = _textMeasurer.GetLineHeight();
    }

    private void UpdateTotalHeight()
    {
        TotalHeight = LineCount * LineHeight;
        OnPropertyChanged(nameof(TotalHeight));
        UpdateScrollOffset(_scrollOffset);
    }

    public void UpdateGutterWidth()
    {
        var currentDigitCount = (int)Math.Log10(Math.Max(2, LineCount)) + 1;
        if (currentDigitCount > _lastMeasuredDigitCount)
        {
            var widestNumber = new string('9', currentDigitCount);
            var textWidth = _textMeasurer.GetStringWidth(widestNumber);
            GutterWidth = textWidth + 40;
            _lastMeasuredDigitCount = currentDigitCount;
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}