using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Config;

namespace meteor.UI.Controls;

public class EditorContentControl : Control
{
    private readonly IEditorViewModel _viewModel;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;
    private readonly AvaloniaEditorConfig _avaloniaConfig;
    
    private readonly double _lineHeight;
    private Size _totalSize;
    private readonly List<int> _lineStartOffsets;

    public Vector Offset { get; set; }
    public Size Viewport { get; set; }

    public EditorContentControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer, IEditorConfig config)
    {
        _config = config;
        _avaloniaConfig = new AvaloniaEditorConfig();
        
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _lineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * _config.LineHeightMultiplier;
        _lineStartOffsets = new List<int>();

        _viewModel.ContentChanged += (sender, e) =>
        {
            UpdateLineStartOffsets();
            InvalidateVisual();
        };
        _viewModel.SelectionChanged += (sender, e) => InvalidateVisual();
        UpdateLineStartOffsets();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        UpdateContentMeasurements();
        return _totalSize;
    }

    private void UpdateContentMeasurements()
    {
        var lineCount = _viewModel.GetLineCount();
        var maxLineWidth = _viewModel.GetMaxLineWidth();
        _totalSize = new Size(maxLineWidth, lineCount * _lineHeight);
    }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(_avaloniaConfig.BackgroundBrush, null, new Rect(Bounds.Size));

        var startLine = Math.Max(0, (int)(Offset.Y / _lineHeight));
        var visibleLines = (int)Math.Ceiling(Viewport.Height / _lineHeight) + 1;
        var endLine = Math.Min(_viewModel.GetLineCount() - 1, startLine + visibleLines);

        var bufferLines = 10;
        var fetchStartLine = Math.Max(0, startLine - bufferLines);
        var fetchEndLine = Math.Min(_viewModel.GetLineCount() - 1, endLine + bufferLines);

        var visibleContent = _viewModel.GetContentSlice(fetchStartLine, fetchEndLine);
        var lines = visibleContent.Split('\n');

        var textHeight = _textMeasurer.MeasureText("Xypg", _config.FontFamily, _config.FontSize).Height;
        var verticalOffset = (_lineHeight - textHeight) / 2;

        var currentLine = _viewModel.GetCursorLine();

        context.PushClip(new Rect(Offset.X, Offset.Y, Viewport.Width, Viewport.Height));
        
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            var lineY = (fetchStartLine + i) * _lineHeight;

            // Highlight the current line
            if (fetchStartLine + i == currentLine)
            {
                context.DrawRectangle(_avaloniaConfig.CurrentLineHighlightBrush, null,
                    new Rect(0, lineY, Bounds.Width, _lineHeight));
            }

            // Render selection
            RenderSelection(context, fetchStartLine + i, line, lineY);

            var formattedText = new FormattedText(
                line,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _avaloniaConfig.Typeface,
                _config.FontSize,
                _avaloniaConfig.TextBrush);

            var textY = lineY + verticalOffset;
            context.DrawText(formattedText, new Point(0, textY));
        }

        // Render cursor 
        RenderCursor(context, currentLine, _viewModel.GetCursorColumn());
    }

    private void RenderCursor(DrawingContext context, int cursorLine, int cursorColumn)
    {
        var cursorY = cursorLine * _lineHeight;
        var cursorX = MeasureTextWidth(_viewModel.GetContentSlice(cursorLine, cursorLine).Substring(0, cursorColumn));

        context.DrawLine(
            new Pen(_avaloniaConfig.TextBrush),
            new Point(cursorX, cursorY),
            new Point(cursorX, cursorY + _lineHeight)
        );
    }

    private void UpdateLineStartOffsets()
    {
        _lineStartOffsets.Clear();
        var content = _viewModel.GetEntireContent();
        var lines = content.Split('\n');
        var totalLength = 0;

        foreach (var line in lines)
        {
            _lineStartOffsets.Add(totalLength);
            totalLength += line.Length + 1; // +1 for newline character
        }
    }


    private void RenderSelection(DrawingContext context, int lineIndex, string line, double lineY)
    {
        var selectionStart = _viewModel.SelectionStart;
        var selectionEnd = _viewModel.SelectionEnd;

        // No selection or selection doesn't intersect this line
        if (selectionStart == selectionEnd || selectionEnd <= selectionStart)
            return;

        // Calculate the start offset for the current line
        var lineStartOffset = _lineStartOffsets[lineIndex];

        if (selectionEnd <= lineStartOffset || selectionStart >= lineStartOffset + line.Length)
            return;

        var relativeSelectionStart = Math.Max(0, selectionStart - lineStartOffset);
        var relativeSelectionEnd = Math.Min(line.Length, selectionEnd - lineStartOffset);

        if (relativeSelectionStart < relativeSelectionEnd || string.IsNullOrEmpty(line))
        {
            var startX = MeasureTextWidth(line.Substring(0, relativeSelectionStart));
            var endX = MeasureTextWidth(line.Substring(0, relativeSelectionEnd));

            if (string.IsNullOrEmpty(line))
            {
                startX = 0;
                endX = 10;
            }

            if (startX < endX)
            {
                var selectionBrush = _avaloniaConfig.SelectionBrush;
                context.DrawRectangle(selectionBrush, null, new Rect(startX, lineY, endX - startX, _lineHeight));
            }
        }
    }

    private double MeasureTextWidth(string text)
    {
        return _textMeasurer.MeasureText(text, _config.FontFamily, _config.FontSize).Width;
    }
}