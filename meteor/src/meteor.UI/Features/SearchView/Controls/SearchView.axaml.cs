using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using meteor.Core.Interfaces;
using meteor.Core.Models;
using meteor.UI.Features.SearchView.ViewModels;
using Color = Avalonia.Media.Color;
using Point = Avalonia.Point;
using Size = Avalonia.Size;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using Vector = Avalonia.Vector;

namespace meteor.UI.Features.SearchView.Controls
{
    public partial class SearchView : UserControl
    {
        private const double ItemHeight = 24;
        private const double SearchBoxBottomMargin = 15;
        private const double FileNameFontSize = 13;
        private const double SnippetFontSize = 13;
        private const double ChevronWidth = 16;
        private const double SearchBoxHeight = 12;

        private readonly ISearchService _searchService;
        private readonly IThemeManager _themeManager;
        private SearchViewModel _viewModel;
        private Canvas _canvas;
        private ScrollViewer _scrollViewer;
        private SearchBox _searchBox;
        private Dictionary<string, double> _cachedItemHeights = new Dictionary<string, double>();

        public SearchView(ISearchService searchService, IThemeManager themeManager)
        {
            _searchService = searchService;
            _themeManager = themeManager;
            _viewModel = new SearchViewModel(searchService);
            DataContext = _viewModel;

            InitializeComponent();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            _searchBox = new SearchBox(_themeManager)
            {
                BorderThickness = 1,
                Height = 24,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Margin = new Thickness(15, 10, 15, 10),
            };
            _searchBox.FilterToggled += OnFilterToggled;

            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _canvas = new Canvas();
            _scrollViewer.Content = _canvas;

            Content = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*"),
                Children =
                {
                    new Border { Child = _searchBox, [Grid.RowProperty] = 0 },
                    new Border { Child = _scrollViewer, [Grid.RowProperty] = 1 }
                }
            };

            UpdateCanvasSize();
        }

        private void SetupEventHandlers()
        {
            _searchBox.OnSearchQueryChanged += async (query) => await PerformSearch();
            _scrollViewer.ScrollChanged += (_, _) => InvalidateVisual();
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerExited += OnPointerExited;
            KeyDown += OnKeyDown;
            _viewModel.PropertyChanged += (_, _) => InvalidateVisual();
            _themeManager.ThemeChanged += OnThemeChanged;
            _viewModel.FilterToggled += async (_, _) => await PerformSearch();
        }

        private void OnThemeChanged(object sender, Theme newTheme)
        {
            InvalidateVisual();
        }

        private async Task PerformSearch()
        {
            _viewModel.SearchQuery = _searchBox.Text;
            if (string.IsNullOrWhiteSpace(_viewModel.SearchQuery))
            {
                _viewModel.ClearResults();
            }
            else
            {
                await _viewModel.ExecuteSearchCommand.ExecuteAsync(null);
            }
            UpdateCanvasSize();
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (!IsVisible) return;

            var searchBoxHeight = _searchBox.Bounds.Height + SearchBoxBottomMargin;
            var viewportRect = new Rect(new Point(0, searchBoxHeight), new Size(_scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height + SearchBoxBottomMargin));

            // Draw background for the search view
            context.FillRectangle(new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor)), new Rect(0, 0, _scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height));

            using (context.PushClip(viewportRect))
            {
                context.FillRectangle(new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor)), viewportRect);

                double yOffset = searchBoxHeight - _scrollViewer.Offset.Y;
                RenderVisibleItems(context, yOffset, viewportRect);
            }
        }

        private void RenderVisibleItems(DrawingContext context, double startY, Rect viewport)
        {
            if (_viewModel.GroupedItems == null || _viewModel.GroupedItems.Count == 0) return;

            double y = startY;
            var textBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor));

            foreach (var group in _viewModel.GroupedItems)
            {
                if (y > viewport.Bottom) break;

                double groupHeight = CalculateGroupHeight(group);
                if (y + groupHeight < viewport.Top)
                {
                    y += groupHeight;
                    continue;
                }

                RenderFileHeader(context, group.Key, y, textBrush);
                y += ItemHeight;

                if (!_viewModel.CollapsedGroups.Contains(group.Key))
                {
                    foreach (var item in group.Value)
                    {
                        double itemHeight = GetCachedItemHeight(item);
                        if (IsItemVisible(y, itemHeight, viewport))
                        {
                            RenderItem(context, item, y, itemHeight, textBrush);
                        }

                        y += itemHeight;
                        if (y > viewport.Bottom) break;
                    }
                }
            }
        }

        private double CalculateGroupHeight(KeyValuePair<string, List<SearchResult>> group)
        {
            if (_viewModel.CollapsedGroups.Contains(group.Key))
            {
                return ItemHeight;
            }
            return ItemHeight + group.Value.Sum(item => GetCachedItemHeight(item));
        }

        private bool IsItemVisible(double itemY, double itemHeight, Rect viewport)
        {
            return itemY + itemHeight > viewport.Top && itemY < viewport.Bottom;
        }

        private void RenderFileHeader(DrawingContext context, string fileName, double y, IBrush textBrush)
        {
            var isCollapsed = _viewModel.CollapsedGroups.Contains(fileName);
            var chevronChar = isCollapsed ? "\uf078" : "\uf054"; // chevron-down : chevron-right

            RenderChevron(context, chevronChar, y, textBrush);
            RenderFileName(context, fileName, y, textBrush);
            RenderHeaderHoverEffect(context, fileName, y);
        }

        private void RenderChevron(DrawingContext context, string chevronChar, double y, IBrush textBrush)
        {
            var formattedText = new FormattedText(
                chevronChar,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free"),
                10,
                textBrush
            );

            double chevronX = 4 + (ChevronWidth - formattedText.Width) / 2 - _scrollViewer.Offset.X;
            double chevronY = y + (ItemHeight - formattedText.Height) / 2;
            context.DrawText(formattedText, new Point(chevronX, chevronY));
        }

        private void RenderFileName(DrawingContext context, string fileName, double y, IBrush textBrush)
        {
            var formattedText = new FormattedText(
                fileName ?? "No file name",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("San Francisco"),
                FileNameFontSize,
                textBrush
            );

            double textY = y + (ItemHeight - formattedText.Height) / 2;
            context.DrawText(formattedText, new Point(ChevronWidth + 5 - _scrollViewer.Offset.X, textY));
        }

        private void RenderHeaderHoverEffect(DrawingContext context, string fileName, double y)
        {
            if (fileName == _viewModel.HoveredHeader)
            {
                var hoverRect = new Rect(-_scrollViewer.Offset.X, y, _scrollViewer.Viewport.Width, ItemHeight);
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)), hoverRect);
            }
        }

        private void RenderItem(DrawingContext context, SearchResult item, double y, double itemHeight, IBrush textBrush)
        {
            var maxWidth = _scrollViewer.Viewport.Width - ChevronWidth;
            var snippet = TruncateText(item.SurroundingContext?.TrimStart() ?? "No snippet available", maxWidth, SnippetFontSize);

            RenderItemHoverEffect(context, item, y, itemHeight);
            RenderItemSnippet(context, snippet, y, itemHeight, textBrush);
            SetItemTooltip(item);
        }

        private void RenderItemHoverEffect(DrawingContext context, SearchResult item, double y, double itemHeight)
        {
            if (item == _viewModel.HoveredItem)
            {
                var hoverRect = new Rect(-_scrollViewer.Offset.X, y, _scrollViewer.Viewport.Width, itemHeight);
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)), hoverRect);
            }
        }

        private void RenderItemSnippet(DrawingContext context, string snippet, double y, double itemHeight, IBrush textBrush)
        {
            var formattedText = new FormattedText(
                snippet,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("San Francisco", FontStyle.Normal, FontWeight.Normal),
                SnippetFontSize,
                textBrush
            );

            double textY = y + (itemHeight - formattedText.Height) / 2;
            context.DrawText(formattedText, new Point(ChevronWidth - _scrollViewer.Offset.X, textY));
        }

        private void SetItemTooltip(SearchResult item)
        {
            ToolTip.SetTip(this, new ToolTip
            {
                Content = item.FilePath,
                Background = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor)),
                Foreground = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor))
            });
        }

        private string TruncateText(string text, double maxWidth, double fontSize)
        {
            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("San Francisco", FontStyle.Normal, FontWeight.Normal),
                fontSize,
                new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor))
            );

            if (formattedText.Width <= maxWidth)
            {
                return text.Replace(Environment.NewLine, " ").Replace("\n", " ");
            }

            int low = 0;
            int high = text.Length;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                string truncated = text.Substring(0, mid).Replace(Environment.NewLine, " ").Replace("\n", " ") + "...";
                formattedText = new FormattedText(
                    truncated,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("San Francisco", FontStyle.Normal, FontWeight.Normal),
                    fontSize,
                    new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor))
                );
                if (formattedText.Width <= maxWidth)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return text.Substring(0, low).Replace(Environment.NewLine, " ").Replace("\n", " ") + "...";
        }

        private double GetCachedItemHeight(SearchResult item)
        {
            if (!_cachedItemHeights.TryGetValue(item.Id, out double height))
            {
                height = ItemHeight;
                _cachedItemHeights[item.Id] = height;
            }
            return height;
        }

        private void UpdateCanvasSize()
        {
            _canvas.Height = CalculateTotalHeight(_viewModel.GroupedItems);
            _viewModel.TotalContentHeight = _canvas.Height;
        }

        private double CalculateTotalHeight(Dictionary<string, List<SearchResult>> groupedItems)
        {
            double totalHeight = 0;

            if (groupedItems == null || groupedItems.Count == 0)
            {
                return _scrollViewer.Bounds.Height;
            }

            foreach (var group in groupedItems)
            {
                totalHeight += ItemHeight; // Group header height

                if (!_viewModel.CollapsedGroups.Contains(group.Key))
                {
                    totalHeight += group.Value.Sum(item => GetCachedItemHeight(item));
                }
            }

            return Math.Max(totalHeight, _scrollViewer.Bounds.Height);
        }

        internal async Task UpdateSearchAsync()
        {
            await _viewModel.ExecuteSearchCommand.ExecuteAsync(null);
            UpdateCanvasSize();
            InvalidateVisual();
        }

        internal void UpdateBackground(Theme theme)
        {
            Background = new SolidColorBrush(Color.Parse(theme.BackgroundColor));
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == BoundsProperty)
            {
                UpdateCanvasSize();
                InvalidateVisual();
            }
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetPosition(this);

            // Ignore clicks in the search box area
            if (point.Y <= _searchBox.Bounds.Height + SearchBoxBottomMargin)
            {
                return;
            }

            double y = -_scrollViewer.Offset.Y + _searchBox.Bounds.Height + SearchBoxBottomMargin;

            if (point.X < 0 || point.X > Bounds.Width || point.Y < 0 || point.Y > Bounds.Height)
            {
                return;
            }

            if (_viewModel.GroupedItems == null) return;

            foreach (var group in _viewModel.GroupedItems)
            {
                if (point.Y >= y && point.Y < y + ItemHeight)
                {
                    _viewModel.ToggleGroupCollapse(group.Key);
                    UpdateCanvasSize();
                    InvalidateVisual();
                    return;
                }
                y += ItemHeight;

                if (!_viewModel.CollapsedGroups.Contains(group.Key))
                {
                    foreach (var item in group.Value)
                    {
                        double itemHeight = GetCachedItemHeight(item);
                        if (point.Y >= y && point.Y < y + itemHeight)
                        {
                            _viewModel.SelectedResult = item;
                            InvalidateVisual();
                            OpenSelectedResult();
                            return;
                        }
                        y += itemHeight;
                    }
                }
            }
        }
        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var point = e.GetPosition(this);

            // Ignore hover in search box area
            if (point.Y <= _searchBox.Bounds.Height + SearchBoxBottomMargin)
            {
                // Deselect hovered item
                _viewModel.HoveredItem = null;
                _viewModel.HoveredHeader = null;
                InvalidateVisual();
                return;
            }

            double y = -_scrollViewer.Offset.Y + _searchBox.Bounds.Height + SearchBoxBottomMargin;

            if (_viewModel.GroupedItems == null) return;

            SearchResult newHoveredItem = null;
            string newHoveredHeader = null;

            foreach (var group in _viewModel.GroupedItems)
            {
                if (point.Y >= y && point.Y < y + ItemHeight)
                {
                    newHoveredHeader = group.Key;
                    break;
                }
                y += ItemHeight;

                if (!_viewModel.CollapsedGroups.Contains(group.Key))
                {
                    foreach (var item in group.Value)
                    {
                        double itemHeight = GetCachedItemHeight(item);
                        if (point.Y >= y && point.Y < y + itemHeight)
                        {
                            newHoveredItem = item;
                            break;
                        }
                        y += itemHeight;
                    }
                    if (newHoveredItem != null) break;
                }
            }

            if (_viewModel.HoveredItem != newHoveredItem || _viewModel.HoveredHeader != newHoveredHeader)
            {
                _viewModel.HoveredItem = newHoveredItem;
                _viewModel.HoveredHeader = newHoveredHeader;
                InvalidateVisual();
            }
        }

        private void OnPointerExited(object sender, PointerEventArgs e)
        {
            if (_viewModel.HoveredItem != null || _viewModel.HoveredHeader != null)
            {
                _viewModel.HoveredItem = null;
                _viewModel.HoveredHeader = null;
                InvalidateVisual();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    _viewModel.MoveSelection(-1);
                    EnsureSelectedItemVisible();
                    break;
                case Key.Down:
                    _viewModel.MoveSelection(1);
                    EnsureSelectedItemVisible();
                    break;
                case Key.Enter:
                    OpenSelectedResult();
                    break;
            }
        }

        private void EnsureSelectedItemVisible()
        {
            double y = _searchBox.Bounds.Height + SearchBoxBottomMargin;
            foreach (var group in _viewModel.GroupedItems)
            {
                y += ItemHeight; // File header
                if (!_viewModel.CollapsedGroups.Contains(group.Key))
                {
                    foreach (var item in group.Value)
                    {
                        double itemHeight = GetCachedItemHeight(item);
                        if (item == _viewModel.SelectedResult)
                        {
                            if (y < _scrollViewer.Offset.Y)
                            {
                                _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, y);
                            }
                            else if (y + itemHeight > _scrollViewer.Offset.Y + _scrollViewer.Viewport.Height)
                            {
                                _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, y + itemHeight - _scrollViewer.Viewport.Height);
                            }
                            return;
                        }
                        y += itemHeight;
                    }
                }
            }
        }

        private void OpenSelectedResult()
        {
            if (_viewModel.SelectedResult != null)
            {
                string filePath = _viewModel.SelectedResult.FilePath;
                int lineNumber = _viewModel.SelectedResult.LineNumber;

                FileSelected?.Invoke(this, new FileSelectedEventArgs(filePath, lineNumber));
            }
        }

        public event EventHandler<FileSelectedEventArgs> FileSelected;

        public class FileSelectedEventArgs : EventArgs
        {
            public string FilePath { get; }
            public int LineNumber { get; }

            public FileSelectedEventArgs(string filePath, int lineNumber)
            {
                FilePath = filePath;
                LineNumber = lineNumber;
            }
        }

        internal void SetSearchDirectory(string path)
        {
            _viewModel.SearchQuery = path;
            _viewModel.ExecuteSearchCommand.ExecuteAsync(null);
            UpdateCanvasSize();
            InvalidateVisual();
        }

        private void OnFilterToggled(object sender, FilterToggledEventArgs e)
        {
            _viewModel.UpdateFilter(e.FilterName, e.IsActive);
            _ = PerformSearch();
        }
    }
}