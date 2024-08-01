using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
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
        private const double LeftPadding = 0;
        private const double RightPadding = 0;
        private const double SearchBoxBottomMargin = 15;
        private const double FileNameFontSize = 13;
        private const double SnippetFontSize = 13;
        private const double ItemSpacing = 0;
        private const double ItemIndentation = 20;
        private const double ChevronWidth = 16;
        private const double SearchBoxHeight = 12;

        private readonly ISearchService _searchService;
        private readonly IThemeManager _themeManager;
        private SearchViewModel _viewModel;
        private Canvas _canvas;
        private ScrollViewer _scrollViewer;
        private TextBox _searchBox;
        private Dictionary<string, List<SearchResult>> _groupedItems;
        private HashSet<string> _collapsedGroups = new HashSet<string>();
        private double _totalContentHeight;
        private FormattedText _cachedFormattedText;
        private Dictionary<string, double> _cachedItemHeights = new Dictionary<string, double>();
        private SearchResult _hoveredItem;
        private string _hoveredHeader;

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
            _searchBox = new TextBox
            {
                Name = "SearchBox",
                Margin = new Thickness(10, 10, 10, SearchBoxBottomMargin),
                Watermark = "Search",
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Height = SearchBoxHeight,
            };
            _searchBox.Classes.Add("default-style");
            UpdateSearchBoxStyles();

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
            _searchBox.TextChanged += async (_, _) => await PerformSearch();
            _scrollViewer.ScrollChanged += (_, _) => InvalidateVisual();
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerExited += OnPointerExited;
            KeyDown += OnKeyDown;
            _viewModel.PropertyChanged += (_, _) => InvalidateVisual();
            _themeManager.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object sender, Theme newTheme)
        {
            UpdateSearchBoxStyles();
            InvalidateVisual();
        }

        private void UpdateSearchBoxStyles()
        {
            _searchBox.Styles.Clear();
            _searchBox.Styles.Add(new Style(selector: x => x.OfType<TextBox>().Class("default-style"))
            {
                Setters =
                {
                    new Setter(TextBox.HeightProperty, SearchBoxHeight),
                    new Setter(ForegroundProperty, new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor)))
                }
            });

            _searchBox.Styles.Add(new Style(selector: x => x.OfType<TextBox>().Class("default-style").Template().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.FileExplorerSelectedItemBackgroundColor))),
                    new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BorderBrush))),
                    new Setter(Border.CornerRadiusProperty, new CornerRadius(4)),
                    new Setter(Border.BorderThicknessProperty, new Thickness(1)),
                }
            });

            _searchBox.Styles.Add(new Style(selector: x => x.OfType<TextBox>().Class(":focus").Template().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.FileExplorerSelectedItemBackgroundColor))),
                    new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BorderBrush))),
                    new Setter(Border.CornerRadiusProperty, new CornerRadius(4)),
                    new Setter(Border.BorderThicknessProperty, new Thickness(1)),
                    new Setter(ForegroundProperty, new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor)))
                }
            });

            _searchBox.Styles.Add(new Style(selector: x => x.OfType<TextBox>().Class(":pointerover").Template().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.FileExplorerSelectedItemBackgroundColor))),
                    new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BorderBrush))),
                    new Setter(Border.CornerRadiusProperty, new CornerRadius(4)),
                    new Setter(Border.BorderThicknessProperty, new Thickness(1)),
                    new Setter(Border.PaddingProperty, new Thickness(4)),
                    new Setter(ForegroundProperty, new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor)))
                }
            });
        }

        private async Task PerformSearch()
        {
            _viewModel.SearchQuery = _searchBox.Text;
            await _viewModel.ExecuteSearchCommand.ExecuteAsync(null);
            UpdateGroupedItems();
            UpdateCanvasSize();
            InvalidateVisual();
        }

        private void UpdateGroupedItems()
        {
            _groupedItems = _viewModel.SearchResults
                .GroupBy(r => r.FileName)
                .ToDictionary(g => g.Key, g => g.ToList());
            _totalContentHeight = CalculateTotalHeight(_groupedItems);
            _cachedItemHeights.Clear(); // Clear the cache when updating items
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (!IsVisible) return;

            var searchBoxHeight = _searchBox.Bounds.Height + SearchBoxBottomMargin;
            var viewportRect = new Rect(new Point(0, searchBoxHeight), new Size(_scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height + SearchBoxBottomMargin));

            using (context.PushClip(viewportRect))
            {
                context.FillRectangle(new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor)), viewportRect);

                double yOffset = searchBoxHeight - _scrollViewer.Offset.Y;
                RenderVisibleItems(context, yOffset, viewportRect);
            }
        }

        private void RenderVisibleItems(DrawingContext context, double startY, Rect viewport)
        {
            if (_groupedItems == null || _groupedItems.Count == 0) return;

            double y = startY;
            var textBrush = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor));

            foreach (var group in _groupedItems)
            {
                if (y > viewport.Bottom) break;

                double groupHeight = CalculateGroupHeight(group);
                if (y + groupHeight < viewport.Top)
                {
                    y += groupHeight;
                    continue;
                }

                RenderFileHeader(context, group.Key, y, textBrush);
                y += ItemHeight + ItemSpacing;

                if (!_collapsedGroups.Contains(group.Key))
                {
                    foreach (var item in group.Value)
                    {
                        double itemHeight = GetCachedItemHeight(item);
                        if (IsItemVisible(y, itemHeight, viewport))
                        {
                            RenderItem(context, item, y, itemHeight, textBrush);
                        }

                        y += itemHeight + ItemSpacing;
                        if (y > viewport.Bottom) break;
                    }
                }
            }
        }

        private double CalculateGroupHeight(KeyValuePair<string, List<SearchResult>> group)
        {
            if (_collapsedGroups.Contains(group.Key))
            {
                return ItemHeight + ItemSpacing;
            }
            return ItemHeight + ItemSpacing + group.Value.Sum(item => GetCachedItemHeight(item) + ItemSpacing);
        }

        private bool IsItemVisible(double itemY, double itemHeight, Rect viewport)
        {
            return itemY + itemHeight > viewport.Top && itemY < viewport.Bottom;
        }

        private void RenderFileHeader(DrawingContext context, string fileName, double y, IBrush textBrush)
        {
            var isCollapsed = _collapsedGroups.Contains(fileName);
            var chevronChar = isCollapsed ? "\uf078" : "\uf054"; // chevron-down : chevron-right

            // Render chevron
            _cachedFormattedText = new FormattedText(
                chevronChar,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free"),
                10,
                textBrush
            );

            double chevronX = 4 + (ChevronWidth - _cachedFormattedText.Width) / 2 - _scrollViewer.Offset.X;
            double chevronY = y + (ItemHeight - _cachedFormattedText.Height) / 2;
            context.DrawText(_cachedFormattedText, new Point(chevronX, chevronY));

            // Render file name
            _cachedFormattedText = new FormattedText(
                fileName ?? "No file name",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("San Francisco"),
                FileNameFontSize,
                textBrush
            );

            double textY = y + (ItemHeight - _cachedFormattedText.Height) / 2;
            context.DrawText(_cachedFormattedText, new Point(LeftPadding + ChevronWidth + 5 - _scrollViewer.Offset.X, textY));

            // Render hover effect
            if (fileName == _hoveredHeader)
            {
                var hoverRect = new Rect(LeftPadding - _scrollViewer.Offset.X, y, _scrollViewer.Viewport.Width - LeftPadding - RightPadding, ItemHeight);
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)), hoverRect);
            }
        }

        private void RenderItem(DrawingContext context, SearchResult item, double y, double itemHeight, IBrush textBrush)
        {
            var maxWidth = _scrollViewer.Viewport.Width - LeftPadding * 2 - RightPadding - ItemIndentation;
            var snippet = TruncateText(item.SurroundingContext?.TrimStart() ?? "No snippet available", maxWidth, SnippetFontSize);

            // Draw hover effect
            if (item == _hoveredItem)
            {
                var hoverRect = new Rect(LeftPadding - _scrollViewer.Offset.X, y, _scrollViewer.Viewport.Width - LeftPadding - RightPadding, itemHeight);
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)), hoverRect);
            }

            _cachedFormattedText = new FormattedText(
                snippet,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("San Francisco", FontStyle.Normal, FontWeight.Normal),
                SnippetFontSize,
                textBrush
            );

            double textY = y + (itemHeight - _cachedFormattedText.Height) / 2;
            context.DrawText(_cachedFormattedText, new Point(LeftPadding + ItemIndentation - _scrollViewer.Offset.X, textY));

            // Add tooltip for file path
            ToolTip.SetTip(this, new ToolTip
            {
                Content = item.FilePath,
                Background = new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.BackgroundColor)),
                Foreground = textBrush
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
            var maxWidth = Math.Max(CalculateMaxWidth(_groupedItems), _scrollViewer.Bounds.Width);
            _canvas.Width = maxWidth;
            _canvas.Height = CalculateTotalHeight(_groupedItems);
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
                totalHeight += ItemHeight + ItemSpacing; // Group header height

                if (!_collapsedGroups.Contains(group.Key))
                {
                    totalHeight += group.Value.Sum(item => GetCachedItemHeight(item) + ItemSpacing);
                }
            }

            return Math.Max(totalHeight, _scrollViewer.Bounds.Height);
        }

        private double CalculateMaxWidth(Dictionary<string, List<SearchResult>> groupedItems)
        {
            if (groupedItems == null || groupedItems.Count == 0)
            {
                return LeftPadding + RightPadding;
            }

            double maxWidth = 0;
            foreach (var group in groupedItems)
            {
                double groupWidth = MeasureTextWidth(group.Key ?? "No file name", FileNameFontSize, FontWeight.Bold);

                if (!_collapsedGroups.Contains(group.Key))
                {
                    groupWidth = Math.Max(groupWidth, group.Value.Max(item =>
                        MeasureTextWidth(item.FileName, FileNameFontSize) + ItemIndentation));
                }

                maxWidth = Math.Max(maxWidth, groupWidth);
            }

            return Math.Max(
                maxWidth + LeftPadding * 2 + RightPadding + ItemIndentation + ChevronWidth,
                _scrollViewer.Viewport.Width
            );
        }

        private double MeasureTextWidth(string text, double fontSize, FontWeight fontWeight = FontWeight.Normal)
        {
            _cachedFormattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("San Francisco", FontStyle.Normal, fontWeight),
                fontSize,
                new SolidColorBrush(Color.Parse(_themeManager.CurrentTheme.TextColor))
            );
            return _cachedFormattedText.Width;
        }

        internal async Task UpdateSearchAsync()
        {
            await _viewModel.ExecuteSearchCommand.ExecuteAsync(null);
            UpdateGroupedItems();
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
            double y = -_scrollViewer.Offset.Y + _searchBox.Bounds.Height + SearchBoxBottomMargin;

            if (_groupedItems == null) return;

            foreach (var group in _groupedItems)
            {
                if (point.Y >= y && point.Y < y + ItemHeight)
                {
                    ToggleGroupCollapse(group.Key);
                    return;
                }
                y += ItemHeight + ItemSpacing;

                if (!_collapsedGroups.Contains(group.Key))
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
                        y += itemHeight + ItemSpacing;
                    }
                }
            }
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var point = e.GetPosition(this);
            double y = -_scrollViewer.Offset.Y + _searchBox.Bounds.Height + SearchBoxBottomMargin;

            if (_groupedItems == null) return;

            SearchResult newHoveredItem = null;
            string newHoveredHeader = null;

            foreach (var group in _groupedItems)
            {
                if (point.Y >= y && point.Y < y + ItemHeight)
                {
                    newHoveredHeader = group.Key;
                    break;
                }
                y += ItemHeight + ItemSpacing;

                if (!_collapsedGroups.Contains(group.Key))
                {
                    foreach (var item in group.Value)
                    {
                        double itemHeight = GetCachedItemHeight(item);
                        if (point.Y >= y && point.Y < y + itemHeight)
                        {
                            newHoveredItem = item;
                            break;
                        }
                        y += itemHeight + ItemSpacing;
                    }
                    if (newHoveredItem != null) break;
                }
            }

            if (_hoveredItem != newHoveredItem || _hoveredHeader != newHoveredHeader)
            {
                _hoveredItem = newHoveredItem;
                _hoveredHeader = newHoveredHeader;
                InvalidateVisual();
            }
        }

        private void OnPointerExited(object sender, PointerEventArgs e)
        {
            if (_hoveredItem != null || _hoveredHeader != null)
            {
                _hoveredItem = null;
                _hoveredHeader = null;
                InvalidateVisual();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    MoveSelection(-1);
                    break;
                case Key.Down:
                    MoveSelection(1);
                    break;
                case Key.Enter:
                    OpenSelectedResult();
                    break;
            }
        }

        private void MoveSelection(int direction)
        {
            var flatResults = _viewModel.SearchResults.ToList();
            var currentIndex = flatResults.IndexOf(_viewModel.SelectedResult);
            var newIndex = (currentIndex + direction + flatResults.Count) % flatResults.Count;
            _viewModel.SelectedResult = flatResults[newIndex];
            EnsureSelectedItemVisible();
        }

        private void EnsureSelectedItemVisible()
        {
            double y = _searchBox.Bounds.Height + SearchBoxBottomMargin;
            foreach (var group in _groupedItems)
            {
                y += ItemHeight + ItemSpacing; // File header
                if (!_collapsedGroups.Contains(group.Key))
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
                        y += itemHeight + ItemSpacing;
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
            UpdateGroupedItems();
            UpdateCanvasSize();
            InvalidateVisual();
        }

        private void ToggleGroupCollapse(string groupKey)
        {
            if (_collapsedGroups.Contains(groupKey))
            {
                _collapsedGroups.Remove(groupKey);
            }
            else
            {
                _collapsedGroups.Add(groupKey);
            }
            UpdateCanvasSize();
            InvalidateVisual();
        }
    }
}