using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Config;

namespace meteor.UI.Controls;

public class GutterControl : Control
{
    private readonly IEditorViewModel _viewModel;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;
    private readonly AvaloniaEditorConfig _avaloniaConfig;

    private readonly double _lineHeight;
    private Size _totalSize;
    private const int Padding = 25;
    private int _lastLineCount;
    private double _maxLineNumberWidth;
    private FormattedText[] _cachedFormattedTexts;

    public double VerticalOffset { get; private set; }
    public Size Viewport { get; set; }

    public GutterControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer, IEditorConfig config)
    {
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _config = config;
        _avaloniaConfig = new AvaloniaEditorConfig();

        _lineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * _config.LineHeightMultiplier;

        _viewModel.ContentChanged += OnContentChanged;
    }

    private void OnContentChanged(object sender, EventArgs e)
    {
        if (_viewModel.GetLineCount() != _lastLineCount)
            InvalidateMeasure();
        else
            InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var lineCount = _viewModel.GetLineCount();
        if (lineCount != _lastLineCount)
        {
            _lastLineCount = lineCount;
            _maxLineNumberWidth = MeasureTextWidth(lineCount.ToString());
            _totalSize = new Size(_maxLineNumberWidth + Padding * 2, lineCount * _lineHeight);
            UpdateCachedFormattedTexts(lineCount);
        }

        return _totalSize;
    }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(_avaloniaConfig.GutterBackgroundBrush, null, new Rect(Bounds.Size));

        var startLine = Math.Max(0, (int)(VerticalOffset / _lineHeight));
        var visibleLines = (int)Math.Ceiling(Viewport.Height / _lineHeight) + 1;
        var endLine = Math.Min(_viewModel.GetLineCount() - 1, startLine + visibleLines);

        var textHeight = _textMeasurer.MeasureText("0", _config.FontFamily, _config.FontSize).Height;
        var verticalOffset = (_lineHeight - textHeight) / 2;

        for (var i = startLine; i <= endLine; i++)
        {
            var formattedText = _cachedFormattedTexts[i];
            var lineY = i * _lineHeight - VerticalOffset;
            var textY = lineY + verticalOffset;
            var textX = Bounds.Width - formattedText.Width - Padding;

            context.DrawText(formattedText, new Point(textX, textY));
        }
    }

    private double MeasureTextWidth(string text)
    {
        return _textMeasurer.MeasureText(text, _config.FontFamily, _config.FontSize).Width;
    }

    public void UpdateScroll(Vector offset)
    {
        VerticalOffset = offset.Y;
        InvalidateVisual();
    }

    private void UpdateCachedFormattedTexts(int lineCount)
    {
        _cachedFormattedTexts = new FormattedText[lineCount];
        for (var i = 0; i < lineCount; i++)
        {
            var lineNumber = (i + 1).ToString();
            _cachedFormattedTexts[i] = new FormattedText(
                lineNumber,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _avaloniaConfig.Typeface,
                _config.FontSize,
                _avaloniaConfig.GutterTextBrush);
        }
    }
}