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
    private readonly IEditorViewModel _viewModel;
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

    private enum ClickType
    {
        Single,
        Double,
        Triple
    }

    public PointerEventHandler(IEditorViewModel viewModel, IScrollManager scrollManager,
        ITextMeasurer textMeasurer, IEditorConfig config, ITextAnalysisService textAnalysisService)
    {
        _viewModel = viewModel;
        _scrollManager = scrollManager;
        _textMeasurer = textMeasurer;
        _config = config;
        _textAnalysisService = textAnalysisService;
        _clickCount = 0;
        _clickTimer = new Timer(300);
        _clickTimer.Elapsed += ResetClickCount;
    }

    public void HandlePointerPressed(Point point)
    {
        _clickTimer.Stop();
        _clickCount++;
        _clickTimer.Start();

        _lastClickPosition = point;
        var documentPosition = GetDocumentPositionFromPoint(point);
        _clickStartPosition = documentPosition;

        if (_clickCount == 1)
        {
            _clickType = ClickType.Single;
            UpdateCursorPosition(documentPosition, false);
            _viewModel.StartSelection(documentPosition);
            _isDragging = true;
        }
        else if (_clickCount == 2)
        {
            _clickType = ClickType.Double;
            SelectWord(documentPosition);
        }
        else if (_clickCount == 3)
        {
            _clickType = ClickType.Triple;
            SelectLine(documentPosition);
        }
    }

    public void HandlePointerMoved(Point point)
    {
        if (_isDragging)
        {
            var documentPosition = GetDocumentPositionFromPoint(point);
            UpdateCursorPosition(documentPosition, true);

            switch (_clickType)
            {
                case ClickType.Single:
                    _viewModel.UpdateSelection(documentPosition);
                    break;
                case ClickType.Double:
                    ExtendSelectionByWord(documentPosition);
                    break;
                case ClickType.Triple:
                    ExtendSelectionByLine(documentPosition);
                    break;
            }
        }
    }

    public void HandlePointerReleased()
    {
        _isDragging = false;
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

        Console.WriteLine(
            $"Point.Y: {point.Y}, ScrollOffset.Y: {_scrollManager.ScrollOffset.Y}, AdjustedY: {adjustedY}, LineHeight: {lineHeight}, LineIndex: {lineIndex}, ClickX: {clickX}");

        var line = _viewModel.GetContentSlice(lineIndex, lineIndex);
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

    private void ResetClickCount(object? sender, ElapsedEventArgs e)
    {
        _clickCount = 0;
        _clickTimer.Stop();
    }

    private void SelectWord(int position)
    {
        var text = _viewModel.GetContentSlice(0, _viewModel.GetLineCount());
        var start = _textAnalysisService.FindPreviousWordBoundary(text, position);
        var end = _textAnalysisService.FindNextWordBoundary(text, position);

        // Handle empty lines
        if (start == end)
        {
            start = _textAnalysisService.FindStartOfCurrentLine(text, position);
            end = _textAnalysisService.FindEndOfCurrentLine(text, position);
        }

        _viewModel.StartSelection(start);
        _viewModel.UpdateSelection(end);
        _isDragging = true;
    }

    private void ExtendSelectionByWord(int position)
    {
        var text = _viewModel.GetContentSlice(0, _viewModel.GetLineCount());
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

        _viewModel.StartSelection(newStart);
        _viewModel.UpdateSelection(newEnd);
    }

    private void SelectLine(int position)
    {
        var text = _viewModel.GetContentSlice(0, _viewModel.GetLineCount());
        var start = _textAnalysisService.FindStartOfCurrentLine(text, position);
        var end = _textAnalysisService.FindEndOfCurrentLine(text, position);

        // Ensure we select the newline character at the end of the line
        if (end < text.Length && text[end] == '\n') end++;

        _viewModel.StartSelection(start);
        _viewModel.UpdateSelection(end);
        _isDragging = true;
    }

    private void ExtendSelectionByLine(int position)
    {
        var text = _viewModel.GetContentSlice(0, _viewModel.GetLineCount());
        var start = _clickStartPosition < position ? _clickStartPosition : position;
        var end = _clickStartPosition > position ? _clickStartPosition : position;

        var newStart = _textAnalysisService.FindStartOfCurrentLine(text, start);
        var newEnd = _textAnalysisService.FindEndOfCurrentLine(text, end);

        // Ensure we select the newline character at the end of the line
        if (newEnd < text.Length && text[newEnd] == '\n') newEnd++;

        _viewModel.StartSelection(newStart);
        _viewModel.UpdateSelection(newEnd);
    }
}