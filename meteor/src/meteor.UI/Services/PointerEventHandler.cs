using System.Timers;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using Timer = System.Timers.Timer;

namespace meteor.UI.Services;

public class PointerEventHandler : IPointerEventHandler
{
    private readonly IScrollManager _scrollManager;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IEditorConfig _config;
    private readonly ITextAnalysisService _textAnalysisService;

    private int _clickCount;
    private readonly Timer _clickTimer;
    private Point _lastClickPosition;
    private bool _isDragging;
    private int _clickStartPosition;
    private ClickType _clickType;
    private const double ClickDistanceThreshold = 5.0;

    private enum ClickType
    {
        Single,
        Double,
        Triple
    }

    public PointerEventHandler(
        IScrollManager scrollManager,
        ITextMeasurer textMeasurer,
        IEditorConfig config,
        ITextAnalysisService textAnalysisService)
    {
        _scrollManager = scrollManager;
        _textMeasurer = textMeasurer;
        _config = config;
        _textAnalysisService = textAnalysisService;
        _clickCount = 0;
        _clickTimer = new Timer(300);
        _clickTimer.Elapsed += ResetClickCount;
    }

    public void HandlePointerPressed(IEditorViewModel viewModel, Point point)
    {
        var distance =
            Math.Sqrt(Math.Pow(point.X - _lastClickPosition.X, 2) + Math.Pow(point.Y - _lastClickPosition.Y, 2));

        if (distance > ClickDistanceThreshold) _clickCount = 0;

        _clickTimer.Stop();
        _clickCount++;
        _clickTimer.Start();

        _lastClickPosition = point;
        var documentPosition = GetDocumentPositionFromPoint(viewModel, point);
        _clickStartPosition = documentPosition;

        if (_clickCount == 1)
        {
            _clickType = ClickType.Single;
            UpdateCursorPosition(viewModel, documentPosition, false);
            viewModel.StartSelection(documentPosition);
            _isDragging = true;
        }
        else if (_clickCount == 2)
        {
            _clickType = ClickType.Double;
            SelectWord(viewModel, documentPosition);
        }
        else if (_clickCount == 3)
        {
            _clickType = ClickType.Triple;
            SelectLine(viewModel, documentPosition);
        }
    }

    public void HandlePointerMoved(IEditorViewModel viewModel, Point point)
    {
        if (_isDragging)
        {
            var documentPosition = GetDocumentPositionFromPoint(viewModel, point);
            UpdateCursorPosition(viewModel, documentPosition, true);

            switch (_clickType)
            {
                case ClickType.Single:
                    viewModel.UpdateSelection(documentPosition);
                    break;
                case ClickType.Double:
                    ExtendSelectionByWord(viewModel, documentPosition);
                    break;
                case ClickType.Triple:
                    ExtendSelectionByLine(viewModel, documentPosition);
                    break;
            }
        }
    }

    public void HandlePointerReleased(IEditorViewModel viewModel)
    {
        _isDragging = false;
        viewModel.EndSelection();
    }

    private void UpdateCursorPosition(IEditorViewModel viewModel, int documentPosition, bool isSelection)
    {
        viewModel.SetCursorPosition(documentPosition);
        _scrollManager.EnsureLineIsVisible(viewModel.GetCursorLine(), viewModel.GetCursorX(), isSelection);
    }

    private int GetDocumentPositionFromPoint(IEditorViewModel viewModel, Point point)
    {
        var lineHeight = _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize) * 1.5;
        var adjustedY = point.Y + _scrollManager.ScrollOffset.Y;
        var lineIndex = Math.Max(0, (int)Math.Floor(adjustedY / lineHeight));
        var lineStart = viewModel.GetLineStartOffset(lineIndex);
        var clickX = point.X + _scrollManager.ScrollOffset.X;

        var line = viewModel.GetContentSlice(lineIndex, lineIndex);
        var trimmedLine = line.TrimEnd('\r', '\n');

        var lineWidth = string.IsNullOrEmpty(trimmedLine)
            ? 0
            : _textMeasurer.MeasureText(trimmedLine, _config.FontFamily, _config.FontSize).Width;

        if (clickX >= lineWidth) return lineStart + trimmedLine.Length;
        if (clickX < 0) return lineStart;

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

    private void ResetClickCount(object? sender, ElapsedEventArgs e)
    {
        _clickCount = 0;
        _clickTimer.Stop();
    }

    private void SelectWord(IEditorViewModel viewModel, int position)
    {
        var text = viewModel.GetContentSlice(0, viewModel.GetLineCount());
        var start = _textAnalysisService.FindPreviousWordBoundary(text, position);
        var end = _textAnalysisService.FindNextWordBoundary(text, position);

        // Handle empty lines
        if (start == end)
        {
            start = _textAnalysisService.FindStartOfCurrentLine(text, position);
            end = _textAnalysisService.FindEndOfCurrentLine(text, position);
        }

        viewModel.StartSelection(start);
        viewModel.UpdateSelection(end);
        _isDragging = true;
    }

    private void ExtendSelectionByWord(IEditorViewModel viewModel, int position)
    {
        var text = viewModel.GetContentSlice(0, viewModel.GetLineCount());
        var start = _clickStartPosition < position ? _clickStartPosition : position;
        var end = _clickStartPosition > position ? _clickStartPosition : position;

        var newStart = _textAnalysisService.FindPreviousWordBoundary(text, start);
        var newEnd = _textAnalysisService.FindNextWordBoundary(text, end);

        // Handle empty lines
        if (newStart == newEnd)
        {
            newStart = _textAnalysisService.FindStartOfCurrentLine(text, start);
            newEnd = _textAnalysisService.FindEndOfCurrentLine(text, end);
        }

        viewModel.StartSelection(newStart);
        viewModel.UpdateSelection(newEnd);
    }

    private void SelectLine(IEditorViewModel viewModel, int position)
    {
        var text = viewModel.GetContentSlice(0, viewModel.GetLineCount());
        var start = _textAnalysisService.FindStartOfCurrentLine(text, position);
        var end = _textAnalysisService.FindEndOfCurrentLine(text, position);

        // Ensure we select the newline character at the end of the line
        if (end < text.Length && text[end] == '\n') end++;

        viewModel.StartSelection(start);
        viewModel.UpdateSelection(end);
        _isDragging = true;
    }

    private void ExtendSelectionByLine(IEditorViewModel viewModel, int position)
    {
        var text = viewModel.GetContentSlice(0, viewModel.GetLineCount());
        var start = _clickStartPosition < position ? _clickStartPosition : position;
        var end = _clickStartPosition > position ? _clickStartPosition : position;

        var newStart = _textAnalysisService.FindStartOfCurrentLine(text, start);
        var newEnd = _textAnalysisService.FindEndOfCurrentLine(text, end);

        // Ensure we select the newline character at the end of the line
        if (newEnd < text.Length && text[newEnd] == '\n') newEnd++;

        viewModel.StartSelection(newStart);
        viewModel.UpdateSelection(newEnd);
    }
}