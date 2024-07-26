using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;

namespace meteor.UI.Services;

public class PointerEventHandler : IPointerEventHandler
{
    private readonly IEditorViewModel _viewModel;
    private readonly IScrollManager _scrollManager;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;

    public PointerEventHandler(IEditorViewModel viewModel, IScrollManager scrollManager,
        ITextMeasurer textMeasurer, IEditorConfig config)
    {
        _viewModel = viewModel;
        _scrollManager = scrollManager;
        _textMeasurer = textMeasurer;
        _config = config;
    }

    public void HandlePointerPressed(Point point)
    {
        var documentPosition = GetDocumentPositionFromPoint(point);
        UpdateCursorPosition(documentPosition, false);
        _viewModel.StartSelection(documentPosition);
    }

    public void HandlePointerMoved(Point point)
    {
        var documentPosition = GetDocumentPositionFromPoint(point);
        UpdateCursorPosition(documentPosition, true);
        _viewModel.UpdateSelection(documentPosition);
    }

    public void HandlePointerReleased()
    {
        _viewModel.EndSelection();
    }

    private void UpdateCursorPosition(int documentPosition, bool isSelection)
    {
        _viewModel.SetCursorPosition(documentPosition);
        _scrollManager.EnsureLineIsVisible(_viewModel.GetCursorLine(), _viewModel.GetCursorX(), isSelection);
    }

    private int GetDocumentPositionFromPoint(Point point)
    {
        var lineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * 1.5;
        var adjustedY = point.Y + _scrollManager.ScrollOffset.Y;
        var lineIndex = Math.Max(0, (int)Math.Floor(adjustedY / lineHeight));
        var lineStart = _viewModel.GetLineStartOffset(lineIndex);
        var clickX = point.X + _scrollManager.ScrollOffset.X;

        var line = _viewModel.GetContentSlice(lineIndex, lineIndex);
        var trimmedLine = line.TrimEnd('\r', '\n');

        // Measure the entire line, or assume a minimal width for an empty line
        var lineWidth = string.IsNullOrEmpty(trimmedLine)
            ? 0
            : _textMeasurer.MeasureText(trimmedLine, _config.FontFamily, _config.FontSize).Width;

        // If clickX is beyond the line width, return the end of the line
        if (clickX >= lineWidth) return lineStart + trimmedLine.Length;

        // If clickX is negative, return the start of the line
        if (clickX < 0) return lineStart;

        // Binary search for the closest character
        int left = 0, right = trimmedLine.Length;
        while (left < right)
        {
            var mid = (left + right) / 2;
            var width = _textMeasurer
                .MeasureText(trimmedLine.Substring(0, mid), _config.FontFamily, _config.FontSize).Width;
            if (width < clickX)
                left = mid + 1;
            else
                right = mid;
        }

        var charIndex = left;
        var documentPosition = lineStart + charIndex;

        return documentPosition;
    }

    private int GetCharIndexFromX(string text, double x)
    {
        int left = 0, right = text.Length;
        while (left < right)
        {
            var mid = (left + right) / 2;
            var width = _textMeasurer.MeasureText(text.Substring(0, mid), _config.FontFamily, _config.FontSize).Width;
            if (width < x)
                left = mid + 1;
            else
                right = mid;
        }

        return left;
    }
}