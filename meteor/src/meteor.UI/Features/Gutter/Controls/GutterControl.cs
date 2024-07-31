using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Config;

namespace meteor.UI.Features.Gutter.Controls;

public class GutterControl : Control
{
    private const int Padding = 25;
    private readonly AvaloniaEditorConfig _avaloniaConfig;
    private readonly IEditorConfig _config;

    private readonly double _lineHeight;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorViewModel _viewModel;

    private bool _isSelecting;
    private int _lastLineCount;
    private double _maxLineNumberWidth;
    private int _selectionStartLine;
    private Size _totalSize;

    public GutterControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer, IEditorConfig config)
    {
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _config = config;
        _avaloniaConfig = new AvaloniaEditorConfig();

        _lineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * _config.LineHeightMultiplier;

        _viewModel.ContentChanged += OnContentChanged;
        _viewModel.SelectionChanged += OnSelectionChanged;

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    public double VerticalOffset { get; private set; }
    public Size Viewport { get; set; }

    public event EventHandler<int>? LineSelected;
    public event EventHandler<Vector>? ScrollRequested;

    private void OnContentChanged(object sender, EventArgs e)
    {
        if (_viewModel.GetLineCount() != _lastLineCount)
            InvalidateMeasure();
        else
            InvalidateVisual();
    }

    private void OnSelectionChanged(object sender, EventArgs e)
    {
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

        var currentLine = _viewModel.GetCursorLine();

        for (var i = startLine; i <= endLine; i++)
        {
            var lineY = i * _lineHeight - VerticalOffset;

            // Highlight the current line in the gutter
            if (i == currentLine)
                context.DrawRectangle(_avaloniaConfig.CurrentLineHighlightBrush, null,
                    new Rect(0, lineY, Bounds.Width, _lineHeight));

            var lineNumber = (i + 1).ToString();
            var formattedText = new FormattedText(
                lineNumber,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _avaloniaConfig.Typeface,
                _config.FontSize,
                _avaloniaConfig.GutterTextBrush);

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

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(this);
        var lineIndex = (int)((position.Y + VerticalOffset) / _lineHeight);

        if (lineIndex >= 0 && lineIndex < _viewModel.GetLineCount())
        {
            LineSelected?.Invoke(this, lineIndex);
            _selectionStartLine = lineIndex;
            var startOffset = _viewModel.GetLineStartOffset(lineIndex);
            var endOffset = _viewModel.GetLineEndOffset(lineIndex);

            _viewModel.SetCursorPosition(startOffset);
            _viewModel.StartSelection(startOffset);
            _viewModel.UpdateSelection(endOffset);

            _isSelecting = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isSelecting)
        {
            var position = e.GetPosition(this);
            var lineIndex = (int)((position.Y + VerticalOffset) / _lineHeight);
            lineIndex = Math.Clamp(lineIndex, 0, _viewModel.GetLineCount() - 1);

            int anchorOffset, activeOffset;

            if (lineIndex >= _selectionStartLine)
            {
                // Moving forward
                anchorOffset = _viewModel.GetLineStartOffset(_selectionStartLine);
                activeOffset = _viewModel.GetLineEndOffset(lineIndex);
            }
            else
            {
                // Moving backward
                anchorOffset = _viewModel.GetLineEndOffset(_selectionStartLine);
                activeOffset = _viewModel.GetLineStartOffset(lineIndex);
            }

            _viewModel.StartSelection(anchorOffset);
            _viewModel.UpdateSelection(activeOffset);

            const double scrollThreshold = 20;
            if (position.Y < scrollThreshold)
                ScrollRequested?.Invoke(this, new Vector(0, -_lineHeight));
            else if (position.Y > Bounds.Height - scrollThreshold)
                ScrollRequested?.Invoke(this, new Vector(0, _lineHeight));
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isSelecting = false;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y * _lineHeight * 3;
        ScrollRequested?.Invoke(this,
            new Vector(0, -delta));
    }
}