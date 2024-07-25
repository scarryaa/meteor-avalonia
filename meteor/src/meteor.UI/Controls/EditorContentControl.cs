using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.UI.Controls;

public class EditorContentControl : Control
{
    private readonly IEditorViewModel _viewModel;
    private readonly ITextMeasurer _textMeasurer;
    private const double FontSize = 13;
    private readonly Typeface _typeface = new("Consolas");
    private readonly double _lineHeight;
    private Size _totalSize;

    public Vector Offset { get; set; }
    public Size Viewport { get; set; }

    public EditorContentControl(IEditorViewModel viewModel, ITextMeasurer textMeasurer)
    {
        _viewModel = viewModel;
        _textMeasurer = textMeasurer;
        _lineHeight = _textMeasurer.GetLineHeight(_typeface.FontFamily.ToString(), FontSize);
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
        context.DrawRectangle(Brushes.White, null, new Rect(Bounds.Size));

        var startLine = Math.Max(0, (int)(Offset.Y / _lineHeight));
        var visibleLines = (int)Math.Ceiling(Viewport.Height / _lineHeight) + 1;
        var endLine = Math.Min(_viewModel.GetLineCount() - 1, startLine + visibleLines);

        var bufferLines = 10;
        var fetchStartLine = Math.Max(0, startLine - bufferLines);
        var fetchEndLine = Math.Min(_viewModel.GetLineCount() - 1, endLine + bufferLines);

        var visibleContent = _viewModel.GetContentSlice(fetchStartLine, fetchEndLine);
        var lines = visibleContent.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var formattedText = new FormattedText(
                line,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                FontSize,
                Brushes.Black);

            var y = (fetchStartLine + i) * _lineHeight;
            context.DrawText(formattedText, new Point(0, y));
        }
    }
}