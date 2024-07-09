using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using meteor.ViewModels;

namespace meteor.Views.Services;

public class InputManager
{
    private TextEditorViewModel _viewModel;
    private Point _lastClickPosition;
    private DateTime _lastClickTime;

    private const int DoubleClickTimeThreshold = 300;
    private const int TripleClickTimeThreshold = 600;
    private const double DoubleClickDistanceThreshold = 5;

    public bool IsTripleClickDrag { get; set; }
    public bool IsDoubleClickDrag { get; set; }

    public void UpdateViewModel(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var currentPosition = e.GetPosition((Visual)sender);
        var currentTime = DateTime.Now;

        // Check for triple-click
        if ((currentTime - _lastClickTime).TotalMilliseconds <= TripleClickTimeThreshold &&
            DistanceBetweenPoints(currentPosition, _lastClickPosition) <= DoubleClickDistanceThreshold &&
            (currentTime - _lastClickTime).TotalMilliseconds > DoubleClickTimeThreshold)
        {
            HandleTripleClick(currentPosition);
            IsTripleClickDrag = true;
            e.Handled = true;
            return;
        }

        // Check for double-click
        if ((currentTime - _lastClickTime).TotalMilliseconds <= DoubleClickTimeThreshold &&
            DistanceBetweenPoints(currentPosition, _lastClickPosition) <= DoubleClickDistanceThreshold)
        {
            HandleDoubleClick(currentPosition);
            IsDoubleClickDrag = true;
            e.Handled = true;
            return;
        }

        // Update last click info
        _lastClickPosition = currentPosition;
        _lastClickTime = currentTime;

        if (_viewModel != null)
        {
            var position = _viewModel.TextEditorUtils.GetPositionFromPoint(currentPosition);

            if (position >= _viewModel.TextBuffer.Length)
                position = _viewModel.TextBuffer.Length;

            // Update cursor position and start selection
            _viewModel.CursorPosition = position;
            _viewModel.SelectionManager.SelectionAnchor = position;

            if (!_viewModel.IsSelecting)
                _viewModel.SelectionStart = _viewModel.SelectionEnd = position;
            else
                _viewModel.SelectionEnd = position;
            _viewModel.SelectionManager.UpdateSelection();
            _viewModel.IsSelecting = true;

            e.Handled = true;
            _viewModel.ShouldScrollToCursor = true;
        }
    }

    public void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_viewModel != null)
        {
            if (_viewModel.IsSelecting || IsDoubleClickDrag || IsTripleClickDrag)
            {
                var position = _viewModel.TextEditorUtils.GetPositionFromPoint(e.GetPosition((Visual)sender));

                if (IsTripleClickDrag)
                    HandleTripleClickDrag(position);
                else if (IsDoubleClickDrag)
                    HandleDoubleClickDrag(position);
                else
                    HandleNormalDrag(position);

                e.Handled = true;
                
                if (!_viewModel.ScrollManager.IsManualScrolling) _viewModel.ScrollManager.ScrollTimer.Start();
            }
            else
            {
                _viewModel.ScrollManager.ScrollTimer.Stop();
                _viewModel.ScrollManager.CurrentScrollSpeed = ScrollManager.ScrollSpeed;
            }
        }
    }

    public void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel._scrollableViewModel == null) return;

        _viewModel.ScrollManager.IsManualScrolling = true;
        var delta = e.Delta.Y * 3;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            // Horizontal scrolling (shift + scroll)
            var newOffset = _viewModel._scrollableViewModel.HorizontalOffset -
                            delta * _viewModel._scrollableViewModel.TextEditorViewModel.CharWidth;
            var maxOffset = Math.Max(0,
                _viewModel._scrollableViewModel.LongestLineWidth - _viewModel._scrollableViewModel.Viewport.Width);
            _viewModel._scrollableViewModel.HorizontalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));
        }
        else
        {
            // Vertical scrolling
            var newOffset = _viewModel._scrollableViewModel.VerticalOffset -
                            delta * _viewModel._scrollableViewModel.LineHeight;
            var maxOffset = Math.Max(0,
                _viewModel.LineCount * _viewModel.LineHeight - _viewModel._scrollableViewModel.Viewport.Height + 6);
            _viewModel._scrollableViewModel.VerticalOffset = Math.Max(0, Math.Min(newOffset, maxOffset));

            if (_viewModel._scrollableViewModel.TextEditorViewModel.IsSelecting)
            {
                // Update selection based on new scroll position
                var position = _viewModel.TextEditorUtils.GetPositionFromPoint(e.GetPosition((Visual)sender));
                _viewModel.SelectionManager.UpdateSelectionDuringManualScroll(position);
            }
        }

        e.Handled = true;

        // Use a dispatcher to reset the manual scrolling flag after a short delay
        Dispatcher.UIThread.Post(() => _viewModel.ScrollManager.IsManualScrolling = false,
            DispatcherPriority.Background);
    }

    public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.IsSelecting = false;
            _viewModel.ScrollManager.DisableHorizontalScrollToCursor = false;
            _viewModel.ScrollManager.DisableVerticalScrollToCursor = false;
            _viewModel.ScrollManager.ScrollTimer.Stop();
            _viewModel.ScrollManager.CurrentScrollSpeed = ScrollManager.ScrollSpeed;
        }

        IsTripleClickDrag = false;
        IsDoubleClickDrag = false;

        e.Handled = true;
    }

    public async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel == null)
        {
            Console.WriteLine("Warning: InputManager's view model is null. Cannot handle key down event.");
            return;
        }

        var handled = false;

        switch (e.Key)
        {
            case Key.Left:
                _viewModel.CursorManager.MoveCursorLeft(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                handled = true;
                break;
            case Key.Right:
                _viewModel.CursorManager.MoveCursorRight(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                handled = true;
                break;
            case Key.Up:
                if (_viewModel.IsPopupVisible)
                {
                    _viewModel.CompletionPopupViewModel.FocusPopup();
                    _viewModel.CompletionPopupViewModel.SelectPreviousItem();
                }
                else
                {
                    _viewModel.CursorManager.MoveCursorUp(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                }

                handled = true;
                break;
            case Key.Down:
                if (_viewModel.IsPopupVisible)
                {
                    _viewModel.CompletionPopupViewModel.FocusPopup();
                    _viewModel.CompletionPopupViewModel.SelectNextItem();
                }
                else
                {
                    _viewModel.CursorManager.MoveCursorDown(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                }

                handled = true;
                break;
            case Key.Home:
                _viewModel.CursorManager.MoveCursorToLineStart(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                handled = true;
                break;
            case Key.End:
                _viewModel.CursorManager.MoveCursorToLineEnd(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                handled = true;
                break;
            case Key.Back:
                await _viewModel.TextManipulator.HandleBackspaceAsync();
                _viewModel.UpdateLineCacheAfterDeletion(_viewModel.CursorPosition + 1, 1);
                handled = true;
                break;
            case Key.Delete:
                await _viewModel.TextManipulator.HandleDeleteAsync();
                _viewModel.UpdateLineCacheAfterDeletion(_viewModel.CursorPosition, 1);
                handled = true;
                break;
            case Key.Enter:
                if (_viewModel.IsPopupVisible && _viewModel.CompletionPopupViewModel.IsFocused)
                {
                    _viewModel.ApplySelectedSuggestion(_viewModel.CompletionPopupViewModel
                        .SelectedItem);
                    handled = true;
                }
                else
                {
                    _viewModel.TextManipulator.InsertNewLine();
                    handled = true;
                }
                break;
            case Key.Tab:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    if (_viewModel.SelectionStart != _viewModel.SelectionEnd)
                        _viewModel.TextManipulator.UnindentSelectionAsync();
                    else
                        _viewModel.TextManipulator.RemoveTab();
                else if (_viewModel.SelectionStart != _viewModel.SelectionEnd)
                    _viewModel.TextManipulator.IndentSelectionAsync();
                else
                    _viewModel.TextManipulator.InsertTab();
                handled = true;
                break;
            case Key.Escape:
                if (_viewModel.IsPopupVisible) _viewModel.HideCompletionSuggestions();
                break;
        }

        // Handle control key combinations
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) handled = HandleControlKeyCombo(e);

        e.Handled = handled;
    }

    public async void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (_viewModel == null)
        {
            Console.WriteLine("Warning: InputManager's view model is null. Cannot handle text input event.");
            return;
        }

        if (!string.IsNullOrEmpty(e.Text))
        {
            _viewModel.HasUserStartedTyping = true;
            await _viewModel.TextManipulator.InsertTextAsync(e.Text);
            _viewModel.UpdateLineCache(_viewModel.CursorPosition - 1, e.Text);
            e.Handled = true;
        }
    }

    private bool HandleControlKeyCombo(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.C:
                _viewModel.ClipboardManager.CopyText();
                return true;
            case Key.V:
                _viewModel.ClipboardManager.PasteText();
                return true;
            case Key.X:
                _viewModel.ClipboardManager.CutText();
                return true;
            case Key.A:
                _viewModel.SelectionManager.SelectAll();
                return true;
            case Key.Z:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    _viewModel.UndoRedoManager.Redo();
                else
                    _viewModel.UndoRedoManager.Undo();
                return true;
            case Key.Left:
                _viewModel.CursorManager.MoveCursorToPreviousWord(_viewModel,
                    e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                break;
            case Key.Right:
                _viewModel.CursorManager.MoveCursorToNextWord(_viewModel, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                break;
            case Key.Up:
                _viewModel.ScrollManager.ScrollViewport(-_viewModel.LineHeight);
                break;
            case Key.Down:
                _viewModel.ScrollManager.ScrollViewport(_viewModel.LineHeight);
                break;
        }

        return false;
    }

    private void HandleDoubleClick(Point position)
    {
        if (_viewModel != null)
        {
            var cursorPosition = _viewModel.TextEditorUtils.GetPositionFromPoint(position);

            // Adjust cursorPosition if it is beyond the line length
            var lineIndex = _viewModel.TextEditorUtils.GetLineIndex(_viewModel, cursorPosition);
            var lineStart = _viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
            var lineText = _viewModel.TextBuffer.GetLineText(lineIndex);
            var lineLength = _viewModel.TextBuffer.GetVisualLineLength((int)lineIndex);

            if (cursorPosition >= lineStart + lineLength)
            {
                // If clicking beyond the end of the line, select the entire line
                _viewModel.SelectionManager.SelectTrailingWhitespace(_viewModel, lineIndex, lineText, lineStart);
                return;
            }

            var (wordStart, wordEnd) =
                _viewModel.TextEditorUtils.FindWordOrSymbolBoundaries(_viewModel, cursorPosition);

            // Ensure the end position does not exceed the length of the text buffer
            wordEnd = Math.Min(wordEnd, _viewModel.TextBuffer.Length);

            // Update selection to encompass the entire word
            _viewModel.SelectionStart = wordStart;
            _viewModel.SelectionEnd = wordEnd;
            _viewModel.SelectionManager.SelectionAnchor = wordStart;
            _viewModel.CursorPosition = wordEnd;

            // Check for word end line index to handle edge cases
            var wordEndLineIndex = _viewModel.TextEditorUtils.GetLineIndex(_viewModel, wordEnd);
            if (wordEnd == _viewModel.TextBuffer.GetLineStartPosition((int)wordEndLineIndex))
            {
                var (_wordStart, _wordEnd) =
                    _viewModel.TextEditorUtils.FindWordOrSymbolBoundaries(_viewModel, cursorPosition);
                _viewModel.SelectionEnd = _wordEnd - 1;
                _viewModel.CursorPosition = _wordEnd - 1;
            }

            _viewModel.IsSelecting = true;
            _viewModel.SelectionManager.UpdateSelection();
        }
    }

    private void HandleTripleClick(Point position)
    {
        if (_viewModel != null)
        {
            var cursorPosition = _viewModel.TextEditorUtils.GetPositionFromPoint(position);

            var lineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(cursorPosition);
            var lineStart = _viewModel.TextBuffer.GetLineStartPosition((int)lineIndex);
            var lineLength = _viewModel.TextBuffer.GetVisualLineLength((int)lineIndex);
            var lineEnd = lineStart + lineLength;

            _viewModel.ShouldScrollToCursor = false;
            _viewModel.SelectionStart = lineStart;
            _viewModel.SelectionEnd = lineEnd;
            _viewModel.SelectionManager.SelectionAnchor = lineStart;
            _viewModel.CursorPosition = lineEnd;

            _viewModel.IsSelecting = true;
            _viewModel.SelectionManager.UpdateSelection();
        }
    }

    private void HandleNormalDrag(long position)
    {
        if (position < _viewModel.SelectionManager.SelectionAnchor)
        {
            _viewModel.SelectionStart = position;
            _viewModel.SelectionEnd = _viewModel.SelectionManager.SelectionAnchor;
        }
        else
        {
            _viewModel.SelectionStart = _viewModel.SelectionManager.SelectionAnchor;
            _viewModel.SelectionEnd = position;
        }

        _viewModel.CursorPosition = position;
        _viewModel.ScrollManager.HandleAutoScrollDuringSelection(
            _viewModel.TextEditorUtils.GetPointFromPosition(_viewModel.CursorPosition));
    }

    private void HandleDoubleClickDrag(long position)
    {
        var (currentWordStart, currentWordEnd) =
            _viewModel.TextEditorUtils.FindWordOrSymbolBoundaries(_viewModel, position);
        var (anchorWordStart, anchorWordEnd) =
            _viewModel.TextEditorUtils.FindWordOrSymbolBoundaries(_viewModel,
                _viewModel.SelectionManager.SelectionAnchor);

        if (position < _viewModel.SelectionManager.SelectionAnchor)
        {
            _viewModel.SelectionStart = Math.Min(currentWordStart, anchorWordStart);
            _viewModel.SelectionEnd = Math.Max(anchorWordEnd, _viewModel.SelectionManager.SelectionAnchor);
            _viewModel.CursorPosition = currentWordStart;
        }
        else
        {
            _viewModel.SelectionStart = Math.Min(anchorWordStart, _viewModel.SelectionManager.SelectionAnchor);
            _viewModel.SelectionEnd = Math.Max(currentWordEnd, anchorWordEnd);
            _viewModel.CursorPosition = currentWordEnd;
        }

        _viewModel.ScrollManager.HandleAutoScrollDuringSelection(
            _viewModel.TextEditorUtils.GetPointFromPosition(_viewModel.CursorPosition));
    }

    private void HandleTripleClickDrag(long position)
    {
        _viewModel.ShouldScrollToCursor = false;
        var currentLineIndex = _viewModel.TextBuffer.GetLineIndexFromPosition(position);
        var anchorLineIndex =
            _viewModel.TextBuffer.GetLineIndexFromPosition(_viewModel.SelectionManager.SelectionAnchor);

        var currentLineStart = _viewModel.TextBuffer.GetLineStartPosition((int)currentLineIndex);
        var currentLineEnd = currentLineStart + _viewModel.TextBuffer.GetVisualLineLength((int)currentLineIndex);

        var anchorLineStart = _viewModel.TextBuffer.GetLineStartPosition((int)anchorLineIndex);
        var anchorLineEnd = anchorLineStart + _viewModel.TextBuffer.GetVisualLineLength((int)anchorLineIndex);

        if (currentLineIndex < anchorLineIndex)
        {
            _viewModel.SelectionStart = currentLineStart;
            _viewModel.SelectionEnd = anchorLineEnd;
            _viewModel.CursorPosition = currentLineStart;
        }
        else
        {
            _viewModel.SelectionStart = anchorLineStart;
            _viewModel.SelectionEnd = currentLineEnd;
            _viewModel.CursorPosition = currentLineEnd + 1;
            _viewModel.OnInvalidateRequired();
        }

        var cursorPoint = _viewModel.TextEditorUtils.GetPointFromPosition(_viewModel.CursorPosition);
        _viewModel.ScrollManager.HandleAutoScrollDuringSelection(cursorPoint, true);
    }

    private double DistanceBetweenPoints(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }
}