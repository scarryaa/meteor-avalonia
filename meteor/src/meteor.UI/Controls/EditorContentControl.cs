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
    private string[] _lines;
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
        var content = _viewModel.Content;
        _lines = content.Split('\n');
        var width = _lines.Max(line =>
            _textMeasurer.MeasureText(line, _typeface.FontFamily.ToString(), FontSize).Width);
        var height = _lines.Length * _lineHeight;
        _totalSize = new Size(width, height);
    }

    public override void Render(DrawingContext context)
    {
        var clip = context.PushClip(new Rect(Bounds.Size));

        context.DrawRectangle(Brushes.White, null, new Rect(Bounds.Size));

        var startLine = Math.Max(0, (int)(Offset.Y / _lineHeight));
        var endLine = Math.Min(_lines.Length - 1, (int)Math.Ceiling(Offset.Y + Viewport.Height / _lineHeight));

        for (var i = startLine; i <= endLine; i++)
        {
            var line = _lines[i];
            var formattedText = new FormattedText(
                line,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                FontSize,
                Brushes.Black);

            var y = i * _lineHeight;
            context.DrawText(formattedText, new Point(0, y));
        }

        clip.Dispose();
    }
}