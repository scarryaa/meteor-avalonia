using System;
using Avalonia;
using Avalonia.Threading;
using meteor.ViewModels;

namespace meteor.Views.Services;

public class ScrollManager
{
    private TextEditorViewModel _viewModel;
    private ScrollableTextEditorViewModel _scrollableViewModel;
    private const double HorizontalScrollThreshold = 20;
    private const double VerticalScrollThreshold = 20;
    private const double ScrollAcceleration = 1.05;

    public const double ScrollSpeed = 1;
    public DispatcherTimer ScrollTimer { get; set; }
    public bool DisableHorizontalScrollToCursor { get; set; }
    public bool DisableVerticalScrollToCursor { get; set; }
    public double CurrentScrollSpeed { get; set; } = ScrollSpeed;
    private bool _isAutoScrolling;

    public bool IsManualScrolling { get; set; }

    public ScrollManager(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
        ScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        ScrollTimer.Tick += ScrollTimer_Tick;
    }

    public void UpdateViewModel(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
        _scrollableViewModel = _viewModel.GetParentViewModel<ScrollableTextEditorViewModel>();
    }

    public void EnsureCursorVisible()
    {
        if (_scrollableViewModel?.TextEditorViewModel == null ||
            !_scrollableViewModel.TextEditorViewModel.ShouldScrollToCursor) return;

        var viewModel = _scrollableViewModel.TextEditorViewModel;
        var cursorLine = viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition);

        if (cursorLine < 0 || cursorLine >= viewModel.TextBuffer.LineStarts.Count)
            return;

        var cursorColumn = viewModel.CursorPosition - viewModel.TextBuffer.LineStarts[(int)cursorLine];

        if (!DisableVerticalScrollToCursor) AdjustVerticalScroll(cursorLine);

        if (!DisableHorizontalScrollToCursor || viewModel.ShouldScrollToCursor)
            AdjustHorizontalScroll(cursorColumn);
    }

    private void AdjustVerticalScroll(long cursorLine)
    {
        var cursorY = cursorLine * _viewModel.LineHeight;
        var bottomPadding = 5;
        var verticalBufferLines = 0;
        var verticalBufferHeight = verticalBufferLines * _viewModel.LineHeight;

        if (cursorY < _scrollableViewModel!.VerticalOffset + verticalBufferHeight)
            _scrollableViewModel.VerticalOffset = Math.Max(0, cursorY - verticalBufferHeight);
        else if (cursorY + _viewModel.LineHeight + bottomPadding > _scrollableViewModel.VerticalOffset +
                 _scrollableViewModel.Viewport.Height - verticalBufferHeight)
            _scrollableViewModel.VerticalOffset = cursorY + _viewModel.LineHeight + bottomPadding -
                _scrollableViewModel.Viewport.Height + verticalBufferHeight;
    }

    private void AdjustHorizontalScroll(long cursorColumn)
    {
        if (_scrollableViewModel == null) return;

        var cursorX = cursorColumn * _scrollableViewModel.TextEditorViewModel.CharWidth;
        var viewportWidth = _scrollableViewModel.Viewport.Width;
        var currentOffset = _scrollableViewModel.HorizontalOffset;

        var margin = viewportWidth * 0.1;

        if (cursorX < currentOffset + margin)
            _scrollableViewModel.HorizontalOffset = Math.Max(0, cursorX - margin);
        else if (cursorX > currentOffset + viewportWidth - margin)
            _scrollableViewModel.HorizontalOffset = Math.Max(0, cursorX - viewportWidth + margin);
    }

    public void ScrollViewport(double delta)
    {
        if (_scrollableViewModel != null)
        {
            var newOffset = _scrollableViewModel.VerticalOffset + delta;
            var maxOffset = _viewModel.TextBuffer.LineCount * _viewModel.LineHeight -
                            _scrollableViewModel.Viewport.Height;
            _scrollableViewModel.VerticalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));
        }
    }

    public void HandleAutoScroll(Point cursorPoint)
    {
        if (_scrollableViewModel == null) return;

        var needScroll = false;

        if (cursorPoint.Y < VerticalScrollThreshold)
        {
            _scrollableViewModel.VerticalOffset =
                Math.Max(0, _scrollableViewModel.VerticalOffset - CurrentScrollSpeed);
            needScroll = true;
        }
        else if (cursorPoint.Y > _scrollableViewModel.Viewport.Height - VerticalScrollThreshold)
        {
            _scrollableViewModel.VerticalOffset = Math.Min(
                _scrollableViewModel.VerticalOffset + CurrentScrollSpeed,
                _viewModel.TotalHeight - _scrollableViewModel.Viewport.Height);
            needScroll = true;
        }

        if (cursorPoint.X < HorizontalScrollThreshold)
        {
            _scrollableViewModel.HorizontalOffset =
                Math.Max(0, _scrollableViewModel.HorizontalOffset - CurrentScrollSpeed);
            needScroll = true;
        }
        else if (cursorPoint.X > _scrollableViewModel.Viewport.Width - HorizontalScrollThreshold)
        {
            _scrollableViewModel.HorizontalOffset = Math.Min(
                _scrollableViewModel.HorizontalOffset + CurrentScrollSpeed,
                _viewModel.LongestLineWidth - _scrollableViewModel.Viewport.Width);
            needScroll = true;
        }

        if (needScroll)
        {
            CurrentScrollSpeed *= ScrollAcceleration;
            if (!_isAutoScrolling)
            {
                _isAutoScrolling = true;
                ScrollTimer.Start();
            }
        }
        else
        {
            StopAutoScroll();
        }
    }

    public void HandleAutoScrollDuringSelection(Point cursorPoint, bool isTripleClickDrag = false)
    {
        if (_viewModel.ShouldScrollToCursor)
        {
            var horizontalThreshold = isTripleClickDrag ? HorizontalScrollThreshold * 1.5 : HorizontalScrollThreshold;
            var verticalThreshold = isTripleClickDrag ? VerticalScrollThreshold * 1.5 : VerticalScrollThreshold;

            CheckAndScrollHorizontally(cursorPoint.X, horizontalThreshold);
            CheckAndScrollVertically(cursorPoint.Y, verticalThreshold);
        }
    }

    private void CheckAndScrollHorizontally(double cursorX, double threshold)
    {
        if (_scrollableViewModel == null) return;

        var viewportWidth = _scrollableViewModel.Viewport.Width;
        var currentOffset = _scrollableViewModel.HorizontalOffset;

        if (cursorX < threshold)
        {
            // Scroll left
            var newOffset = Math.Max(0, currentOffset - CalculateScrollAmount(cursorX, threshold));
            _scrollableViewModel.HorizontalOffset = newOffset;
        }
        else if (cursorX > viewportWidth - threshold)
        {
            // Scroll right
            var newOffset = currentOffset + CalculateScrollAmount(viewportWidth - cursorX, threshold);
            _scrollableViewModel.HorizontalOffset = newOffset;
        }
        else
        {
            CurrentScrollSpeed = ScrollSpeed; // Reset scroll speed when not near edges
        }
    }

    private void CheckAndScrollVertically(double cursorY, double threshold)
    {
        if (_scrollableViewModel == null) return;

        var viewportHeight = _scrollableViewModel.Viewport.Height;
        var currentOffset = _scrollableViewModel.VerticalOffset;
        var lineCount = _viewModel.TextBuffer.LineCount;

        if (cursorY < threshold)
        {
            // Scroll up
            var scrollAmount = CalculateScrollAmount(cursorY, threshold);
            var newOffset = Math.Max(0, currentOffset - scrollAmount);
            _scrollableViewModel.VerticalOffset = newOffset;

            // Adjust cursor position if necessary
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var firstVisibleLine = (long)(newOffset / _viewModel.LineHeight);
            if (_viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition) < firstVisibleLine)
            {
                var newCursorPosition = viewModel.TextBuffer.GetLineStartPosition((int)firstVisibleLine);
                if (newCursorPosition < viewModel.SelectionStart)
                {
                    viewModel.SelectionStart = newCursorPosition;
                    viewModel.CursorPosition = newCursorPosition;
                }
            }
        }
        else if (cursorY > viewportHeight - threshold)
        {
            // Scroll down
            var scrollAmount = CalculateScrollAmount(viewportHeight - cursorY, threshold);
            var newOffset = currentOffset + scrollAmount;
            var maxOffset = Math.Max(0, lineCount * _viewModel.LineHeight - viewportHeight);
            _scrollableViewModel.VerticalOffset = Math.Min(newOffset, maxOffset);

            // Adjust cursor position if necessary
            var viewModel = _scrollableViewModel.TextEditorViewModel;
            var lastVisibleLine = (long)((newOffset + viewportHeight) / _viewModel.LineHeight);
            if (_viewModel.TextBuffer.GetLineIndexFromPosition(viewModel.CursorPosition) > lastVisibleLine)
            {
                var newCursorPosition =
                    viewModel.TextBuffer.GetLineEndPosition((int)Math.Min(lastVisibleLine, lineCount - 1));
                if (newCursorPosition > viewModel.SelectionEnd)
                {
                    viewModel.SelectionEnd = newCursorPosition;
                    viewModel.CursorPosition = newCursorPosition;
                }
            }
        }
    }

    private double CalculateScrollAmount(double distanceFromEdge, double threshold)
    {
        return Math.Min(CurrentScrollSpeed * (1 - distanceFromEdge / threshold), _viewModel.LineHeight);
    }

    private void ScrollTimer_Tick(object sender, EventArgs e)
    {
        if (_viewModel == null || IsManualScrolling) return;

        var cursorPosition = _viewModel.CursorPosition;
        var cursorPoint = _viewModel.TextEditorUtils.GetPointFromPosition(cursorPosition);

        CheckAndScrollHorizontally(cursorPoint.X, HorizontalScrollThreshold);
        CheckAndScrollVertically(cursorPoint.Y, VerticalScrollThreshold);

        CurrentScrollSpeed *= ScrollAcceleration;
    }

    private void StopAutoScroll()
    {
        ScrollTimer.Stop();
        _isAutoScrolling = false;
        CurrentScrollSpeed = ScrollSpeed;
    }

    public void ScrollToLine(int lineIndex)
    {
        if (_scrollableViewModel == null) return;

        var lineY = lineIndex * _viewModel.LineHeight;
        _scrollableViewModel.VerticalOffset = Math.Max(0, Math.Min(
            lineY,
            _viewModel.TotalHeight - _scrollableViewModel.Viewport.Height));
    }

    public void ScrollToPosition(long position)
    {
        if (_scrollableViewModel == null) return;

        var point = _viewModel.TextEditorUtils.GetPointFromPosition(position);
        _scrollableViewModel.VerticalOffset = Math.Max(0, Math.Min(
            point.Y - _scrollableViewModel.Viewport.Height / 2,
            _viewModel.TotalHeight - _scrollableViewModel.Viewport.Height));
        _scrollableViewModel.HorizontalOffset = Math.Max(0, Math.Min(
            point.X - _scrollableViewModel.Viewport.Width / 2,
            _viewModel.LongestLineWidth - _scrollableViewModel.Viewport.Width));
    }
}