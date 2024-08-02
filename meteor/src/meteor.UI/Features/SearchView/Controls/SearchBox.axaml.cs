using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using meteor.Core.Interfaces;

namespace meteor.UI.Features.SearchView.Controls
{
    public class SearchBox : Control
    {
        private readonly IThemeManager _themeManager;
        private string _searchQuery = string.Empty;
        private FormattedText _formattedText;
        private int _cursorPosition;
        private int _selectionStart;
        private int _selectionEnd;
        private bool _isSelecting;
        private bool _isFocused;
        private List<FilterButton> _filterButtons;

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
            _cursorPosition = 0;
            _selectionStart = 0;
            _selectionEnd = 0;
            _isSelecting = false;
            _isFocused = false;

            _filterButtons = new List<FilterButton>
            {
                new FilterButton("Aa", "Match Case", _themeManager),
                new FilterButton(".*", "Regex", _themeManager),
                new FilterButton("W", "Match Whole Word", _themeManager)
            };

            foreach (var button in _filterButtons)
            {
                button.FilterToggled += OnFilterToggled;
            }

            _themeManager.ThemeChanged += (sender, theme) => UpdateTheme();
        }

        private void UpdateTheme()
        {
            InvalidateVisual();
        }

        public Func<string, Task> OnSearchQueryChanged { get; set; }
        public string Text
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnSearchQueryChanged?.Invoke(_searchQuery);
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var backgroundBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.SearchViewBackgroundColor));
            var borderBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.SearchViewBorderColor));
            var textBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor));

            var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
            var innerRect = rect.Deflate(BorderThickness / 2);

            // Draw background
            context.FillRectangle(backgroundBrush, innerRect);

            // Draw border
            context.DrawRectangle(null, new Pen(borderBrush, BorderThickness), rect);

            _formattedText = CreateFormattedText(_searchQuery);
            var textPosition = new Point(5 + BorderThickness / 2, (innerRect.Height - _formattedText.Height) / 2 + BorderThickness / 2);

            // Draw selection
            if (_selectionStart != _selectionEnd)
            {
                var start = Math.Min(_selectionStart, _selectionEnd);
                var end = Math.Max(_selectionStart, _selectionEnd);
                var selectionStartX = textPosition.X + CreateFormattedText(_searchQuery.Substring(0, start)).Width;
                var selectionEndX = textPosition.X + CreateFormattedText(_searchQuery.Substring(0, end)).Width;
                var selectionRect = new Rect(selectionStartX, textPosition.Y, selectionEndX - selectionStartX, _formattedText.Height);
                context.FillRectangle(new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.SelectionColor)), selectionRect);
            }

            // Draw text
            context.DrawText(_formattedText, textPosition);

            // Draw cursor only if focused
            if (_isFocused)
            {
                var cursorX = textPosition.X + CreateFormattedText(_searchQuery.Substring(0, _cursorPosition)).Width;
                var cursorY = textPosition.Y;
                context.DrawLine(new Pen(textBrush, 1), new Point(cursorX, cursorY), new Point(cursorX, cursorY + _formattedText.Height));
            }

            // Draw filter buttons
            const double buttonWidth = 24;
            const double buttonHeight = 24;
            const double buttonSpacing = 5;
            double totalButtonsWidth = _filterButtons.Count * buttonWidth + (_filterButtons.Count - 1) * buttonSpacing;
            double buttonX = Bounds.Width - totalButtonsWidth - 5;

            for (int i = 0; i < _filterButtons.Count; i++)
            {
                var button = _filterButtons[i];
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
            _selectionStart = _cursorPosition;
            _selectionEnd = _cursorPosition;
            OnSearchQueryChanged?.Invoke(_searchQuery);
            InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus();
            var point = e.GetPosition(this);

            // Check if a filter button was clicked
            var clickedButton = GetClickedFilterButton(point);
            if (clickedButton != null)
            {
                clickedButton.IsActive = !clickedButton.IsActive;
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            _cursorPosition = GetCursorPositionFromPoint(point);
            _selectionStart = _cursorPosition;
            _selectionEnd = _cursorPosition;
            _isSelecting = true;
            InvalidateVisual();
        }

        private FilterButton GetClickedFilterButton(Point point)
        {
            const double buttonWidth = 24;
            const double buttonHeight = 24;
            const double buttonSpacing = 5;
            double totalButtonsWidth = _filterButtons.Count * buttonWidth + (_filterButtons.Count - 1) * buttonSpacing;
            double buttonX = Bounds.Width - totalButtonsWidth - 5;

            for (int i = 0; i < _filterButtons.Count; i++)
            {
                var buttonY = (Bounds.Height - buttonHeight) / 2;
                var buttonRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);

                if (buttonRect.Contains(point))
                {
                    return _filterButtons[i];
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
                    e.Handled = true;
                    break;
                case Key.Delete:
                    HandleDeleteKey();
                    e.Handled = true;
                    break;
                case Key.Left:
                    MoveCursor(-1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    e.Handled = true;
                    break;
                case Key.Right:
                    MoveCursor(1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    e.Handled = true;
                    break;
                case Key.Home:
                    MoveCursorToStart(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    e.Handled = true;
                    break;
                case Key.End:
                    MoveCursorToEnd(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    e.Handled = true;
                    break;
                case Key.A when e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta):
                    SelectAll();
                    e.Handled = true;
                    break;
                case Key.C when e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta):
                    _ = CopySelectionAsync();
                    e.Handled = true;
                    break;
                case Key.X when e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta):
                    _ = CutSelection();
                    e.Handled = true;
                    break;
                case Key.V when e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta):
                    _ = PasteClipboard();
                    e.Handled = true;
                    break;
            }

            // Only raise the event if the text has changed
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                OnSearchQueryChanged?.Invoke(_searchQuery);
                InvalidateVisual();
                RaiseTextChangedEvent();
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
                _selectionStart = _cursorPosition;
                _selectionEnd = _cursorPosition;
            }
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
                _selectionStart = _cursorPosition;
                _selectionEnd = _cursorPosition;
            }
        }

        private void MoveCursor(int offset, bool isShiftPressed)
        {
            _cursorPosition = Math.Clamp(_cursorPosition + offset, 0, _searchQuery.Length);
            UpdateSelection(isShiftPressed);
        }

        private void MoveCursorToStart(bool isShiftPressed)
        {
            _cursorPosition = 0;
            UpdateSelection(isShiftPressed);
        }

        private void MoveCursorToEnd(bool isShiftPressed)
        {
            _cursorPosition = _searchQuery.Length;
            UpdateSelection(isShiftPressed);
        }

        private void UpdateSelection(bool isShiftPressed)
        {
            if (!isShiftPressed)
            {
                _selectionStart = _cursorPosition;
                _selectionEnd = _cursorPosition;
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
                _selectionStart = _cursorPosition;
                _selectionEnd = _cursorPosition;
                OnSearchQueryChanged?.Invoke(_searchQuery);
                InvalidateVisual();
                RaiseTextChangedEvent();
            }
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