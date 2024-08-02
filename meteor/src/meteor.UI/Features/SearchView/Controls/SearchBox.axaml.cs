using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace meteor.UI.Features.SearchView.Controls
{
    public class SearchBox : Control
    {
        private readonly IThemeManager _themeManager;
        private string _searchQuery = string.Empty;
        private FormattedText _formattedText;
        private int _cursorPosition, _selectionStart, _selectionEnd;
        private bool _isSelecting, _isFocused;
        private List<FilterButton> _filterButtons;
        private double _scrollOffset = 0;
        private List<string> _searchHistory = new List<string>();
        private int _historyIndex = -1;
        private System.Timers.Timer _typingTimer;

        public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
            AvaloniaProperty.Register<SearchBox, IBrush>(nameof(BackgroundBrush));

        public static readonly StyledProperty<IBrush> BorderBrushProperty =
            AvaloniaProperty.Register<SearchBox, IBrush>(nameof(BorderBrush));

        public static readonly StyledProperty<double> BorderThicknessProperty =
            AvaloniaProperty.Register<SearchBox, double>(nameof(BorderThickness), 1.0);

        public IBrush BackgroundBrush
        {
            get => GetValue(BackgroundBrushProperty);
            set => SetValue(BackgroundBrushProperty, value);
        }

        public IBrush BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public double BorderThickness
        {
            get => GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public SearchBox(IThemeManager themeManager)
        {
            _themeManager = themeManager;
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            Height = 30;
            Focusable = true;
            _formattedText = CreateFormattedText(_searchQuery);

            _filterButtons = new List<FilterButton>
            {
                new FilterButton("Aa", "Match Case", _themeManager),
                new FilterButton(".*", "Regex", _themeManager),
                new FilterButton("W", "Match Whole Word", _themeManager)
            };

            _filterButtons.ForEach(button => button.FilterToggled += OnFilterToggled);

            _themeManager.ThemeChanged += (sender, theme) => InvalidateVisual();

            _typingTimer = new System.Timers.Timer(1000) { AutoReset = false };
            _typingTimer.Elapsed += (sender, e) => Dispatcher.UIThread.InvokeAsync(AddToSearchHistory);
        }

        public Func<string, Task> OnSearchQueryChanged { get; set; }
        public string Text
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnSearchQueryChanged?.Invoke(_searchQuery);
                ResetTypingTimer();
            }
        }

        private void ResetTypingTimer()
        {
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var backgroundBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.SearchViewBackgroundColor));
            var borderBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.SearchViewBorderColor));
            var textBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor));

            var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
            var innerRect = rect.Deflate(BorderThickness / 2);

            const double buttonWidth = 24, buttonHeight = 24, buttonSpacing = 5;
            double totalButtonsWidth = _filterButtons.Count * (buttonWidth + buttonSpacing) - buttonSpacing;

            var clipRect = new Rect(0, 0, Bounds.Width - totalButtonsWidth - 10, Bounds.Height);

            using (context.PushClip(clipRect))
            {
                context.FillRectangle(backgroundBrush, innerRect);
                context.DrawRectangle(null, new Pen(borderBrush, BorderThickness), rect);

                _formattedText = CreateFormattedText(_searchQuery);
                var textPosition = new Point(5 + BorderThickness / 2 - _scrollOffset, (innerRect.Height - _formattedText.Height) / 2 + BorderThickness / 2);

                if (_selectionStart != _selectionEnd)
                {
                    var start = Math.Min(_selectionStart, _selectionEnd);
                    var end = Math.Max(_selectionStart, _selectionEnd);
                    var selectionStartX = textPosition.X + CreateFormattedText(_searchQuery[..start]).WidthIncludingTrailingWhitespace;
                    var selectionEndX = textPosition.X + CreateFormattedText(_searchQuery[..end]).WidthIncludingTrailingWhitespace;
                    var selectionRect = new Rect(selectionStartX, textPosition.Y, selectionEndX - selectionStartX, _formattedText.Height);
                    context.FillRectangle(new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.SelectionColor)), selectionRect);
                }

                context.DrawText(_formattedText, textPosition);

                if (_isFocused)
                {
                    var cursorX = textPosition.X + CreateFormattedText(_searchQuery[.._cursorPosition]).WidthIncludingTrailingWhitespace;
                    var cursorY = textPosition.Y;
                    context.DrawLine(new Pen(textBrush, 1), new Point(cursorX, cursorY), new Point(cursorX, cursorY + _formattedText.Height));
                }
            }

            double buttonX = Bounds.Width - totalButtonsWidth - 5;

            foreach (var button in _filterButtons)
            {
                var buttonY = (Bounds.Height - buttonHeight) / 2;
                var buttonRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);

                using (context.PushTransform(Matrix.CreateTranslation(buttonRect.X - button.Bounds.X, buttonRect.Y - button.Bounds.Y)))
                {
                    button.Width = buttonWidth;
                    button.Height = buttonHeight;
                    button.Render(context);
                }
                buttonX += buttonWidth + buttonSpacing;
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (_selectionStart != _selectionEnd)
            {
                DeleteSelection();
            }

            _searchQuery = _searchQuery.Insert(_cursorPosition, e.Text);
            _cursorPosition += e.Text.Length;
            _selectionStart = _selectionEnd = _cursorPosition;
            OnSearchQueryChanged?.Invoke(_searchQuery);
            UpdateScrollOffset();
            InvalidateVisual();
            ResetTypingTimer();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus();
            var point = e.GetPosition(this);

            var clickedButton = GetClickedFilterButton(point);
            if (clickedButton != null)
            {
                clickedButton.IsActive = !clickedButton.IsActive;
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            _cursorPosition = _selectionStart = _selectionEnd = GetCursorPositionFromPoint(point);
            _isSelecting = true;
            UpdateScrollOffset();
            InvalidateVisual();
        }

        private FilterButton GetClickedFilterButton(Point point)
        {
            const double buttonWidth = 24, buttonHeight = 24, buttonSpacing = 5;
            double totalButtonsWidth = _filterButtons.Count * (buttonWidth + buttonSpacing) - buttonSpacing;
            double buttonX = Bounds.Width - totalButtonsWidth - 5;

            foreach (var button in _filterButtons)
            {
                var buttonY = (Bounds.Height - buttonHeight) / 2;
                var buttonRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);
                if (buttonRect.Contains(point))
                {
                    return button;
                }
                buttonX += buttonWidth + buttonSpacing;
            }
            return null;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_isSelecting)
            {
                var point = e.GetPosition(this);
                _selectionEnd = GetCursorPositionFromPoint(point);
                UpdateScrollOffset();
                InvalidateVisual();
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _isSelecting = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.Key)
            {
                case Key.Back:
                    HandleBackspaceKey();
                    break;
                case Key.Delete:
                    HandleDeleteKey();
                    break;
                case Key.Left:
                    MoveCursor(-1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    break;
                case Key.Right:
                    MoveCursor(1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    break;
                case Key.Home:
                    MoveCursorToStart(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    break;
                case Key.End:
                    MoveCursorToEnd(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    break;
                case Key.A when e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta):
                    SelectAll();
                    break;
                case Key.C when e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta):
                    _ = CopySelectionAsync();
                    break;
                case Key.X when e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta):
                    _ = CutSelection();
                    break;
                case Key.V when e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta):
                    _ = PasteClipboard();
                    break;
                case Key.Up:
                    NavigateSearchHistory(-1);
                    break;
                case Key.Down:
                    NavigateSearchHistory(1);
                    break;
                case Key.Enter:
                    AddToSearchHistory();
                    break;
                default:
                    return;
            }

            e.Handled = true;

            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                OnSearchQueryChanged?.Invoke(_searchQuery);
                UpdateScrollOffset();
                InvalidateVisual();
                RaiseTextChangedEvent();
                ResetTypingTimer();
            }
        }

        private void NavigateSearchHistory(int direction)
        {
            if (_searchHistory.Count == 0) return;

            _historyIndex = Math.Clamp(_historyIndex + direction, -1, _searchHistory.Count - 1);

            _searchQuery = _historyIndex == -1 ? string.Empty : _searchHistory[_historyIndex];

            _cursorPosition = _searchQuery.Length;
            _selectionStart = _selectionEnd = _cursorPosition;
            OnSearchQueryChanged?.Invoke(_searchQuery);
            UpdateScrollOffset();
            InvalidateVisual();
        }

        private void AddToSearchHistory()
        {
            if (!string.IsNullOrWhiteSpace(_searchQuery) && (_searchHistory.Count == 0 || _searchQuery != _searchHistory[^1]))
            {
                _searchHistory.Add(_searchQuery);
                _historyIndex = _searchHistory.Count;
            }
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            _isFocused = true;
            InvalidateVisual();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            _isFocused = false;
            InvalidateVisual();
        }

        private void HandleBackspaceKey()
        {
            if (_selectionStart != _selectionEnd)
            {
                DeleteSelection();
            }
            else if (_cursorPosition > 0)
            {
                _searchQuery = _searchQuery.Remove(--_cursorPosition, 1);
                _selectionStart = _selectionEnd = _cursorPosition;
            }
            UpdateScrollOffset();
        }

        private void HandleDeleteKey()
        {
            if (_selectionStart != _selectionEnd)
            {
                DeleteSelection();
            }
            else if (_cursorPosition < _searchQuery.Length)
            {
                _searchQuery = _searchQuery.Remove(_cursorPosition, 1);
                _selectionStart = _selectionEnd = _cursorPosition;
            }
            UpdateScrollOffset();
        }

        private void MoveCursor(int offset, bool isShiftPressed)
        {
            _cursorPosition = Math.Clamp(_cursorPosition + offset, 0, _searchQuery.Length);
            UpdateSelection(isShiftPressed);
            UpdateScrollOffset();
        }

        private void MoveCursorToStart(bool isShiftPressed)
        {
            _cursorPosition = 0;
            UpdateSelection(isShiftPressed);
            UpdateScrollOffset();
        }

        private void MoveCursorToEnd(bool isShiftPressed)
        {
            _cursorPosition = _searchQuery.Length;
            UpdateSelection(isShiftPressed);
            UpdateScrollOffset();
        }

        private void UpdateSelection(bool isShiftPressed)
        {
            if (!isShiftPressed)
            {
                _selectionStart = _selectionEnd = _cursorPosition;
            }
            else
            {
                _selectionEnd = _cursorPosition;
            }
        }

        private void SelectAll()
        {
            _selectionStart = 0;
            _selectionEnd = _searchQuery.Length;
            _cursorPosition = _searchQuery.Length;
            UpdateScrollOffset();
            InvalidateVisual();
        }

        private async Task CopySelectionAsync()
        {
            if (_selectionStart != _selectionEnd)
            {
                var start = Math.Min(_selectionStart, _selectionEnd);
                var length = Math.Abs(_selectionStart - _selectionEnd);
                var selectedText = _searchQuery.Substring(start, length);
                await TopLevel.GetTopLevel(this).Clipboard.SetTextAsync(selectedText);
            }
        }

        private async Task CutSelection()
        {
            if (_selectionStart != _selectionEnd)
            {
                await CopySelectionAsync();
                DeleteSelection();
                UpdateScrollOffset();
            }
        }

        private async Task PasteClipboard()
        {
            var clipboardText = await TopLevel.GetTopLevel(this).Clipboard.GetTextAsync();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                if (_selectionStart != _selectionEnd)
                {
                    DeleteSelection();
                }
                _searchQuery = _searchQuery.Insert(_cursorPosition, clipboardText);
                _cursorPosition += clipboardText.Length;
                _selectionStart = _selectionEnd = _cursorPosition;
                OnSearchQueryChanged?.Invoke(_searchQuery);
                UpdateScrollOffset();
                InvalidateVisual();
                RaiseTextChangedEvent();
            }
        }
        private void UpdateScrollOffset()
        {
            if (_formattedText == null)
            {
                _formattedText = CreateFormattedText(_searchQuery);
            }

            double textWidth = _formattedText.WidthIncludingTrailingWhitespace;
            double filterButtonsWidth = CalculateFilterButtonsWidth();
            double visibleWidth = Bounds.Width - BorderThickness * 2 - 20 - filterButtonsWidth;

            if (textWidth <= visibleWidth)
            {
                _scrollOffset = 0;
            }
            else
            {
                double cursorX = CreateFormattedText(_searchQuery.Substring(0, _cursorPosition)).WidthIncludingTrailingWhitespace;

                if (cursorX - _scrollOffset < 0)
                {
                    _scrollOffset = Math.Max(0, cursorX - 10);
                }
                else if (cursorX - _scrollOffset > visibleWidth)
                {
                    _scrollOffset = Math.Min(textWidth - visibleWidth, cursorX - visibleWidth + 10);
                }
            }

            InvalidateVisual();
        }

        private double CalculateFilterButtonsWidth()
        {
            if (_filterButtons == null || _filterButtons.Count == 0)
            {
                return 0;
            }

            const double buttonWidth = 24;
            const double buttonSpacing = 5;
            return _filterButtons.Count * buttonWidth + (_filterButtons.Count - 1) * buttonSpacing + 5;
        }

        private int GetCursorPositionFromPoint(Point point)
        {
            for (int i = 0; i <= _searchQuery.Length; i++)
            {
                var width = CreateFormattedText(_searchQuery.Substring(0, i)).Width;
                if (point.X < 5 + BorderThickness / 2 + width)
                {
                    return i;
                }
            }
            return _searchQuery.Length;
        }

        private void DeleteSelection()
        {
            var start = Math.Min(_selectionStart, _selectionEnd);
            var length = Math.Abs(_selectionStart - _selectionEnd);
            _searchQuery = _searchQuery.Remove(start, length);
            _cursorPosition = start;
            _selectionStart = _cursorPosition;
            _selectionEnd = _cursorPosition;
        }

        private FormattedText CreateFormattedText(string text)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("San Francisco", FontStyle.Normal, FontWeight.Normal),
                12,
                new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor))
            );
        }

        private void RaiseTextChangedEvent()
        {
            var textChangedEventArgs = new RoutedEventArgs(TextChangedEvent);
            RaiseEvent(textChangedEventArgs);
        }

        public static readonly RoutedEvent<RoutedEventArgs> TextChangedEvent =
            RoutedEvent.Register<RoutedEventArgs>("TextChanged", RoutingStrategies.Bubble, typeof(SearchBox));

        public event EventHandler<FilterToggledEventArgs> FilterToggled;

        private void OnFilterToggled(object sender, bool isActive)
        {
            if (sender is FilterButton filterButton)
            {
                FilterToggled?.Invoke(this, new FilterToggledEventArgs(filterButton.Tooltip, isActive));
            }
        }
    }

    public class FilterToggledEventArgs : EventArgs
    {
        public string FilterName { get; }
        public bool IsActive { get; }

        public FilterToggledEventArgs(string filterName, bool isActive)
        {
            FilterName = filterName;
            IsActive = isActive;
        }
    }
}