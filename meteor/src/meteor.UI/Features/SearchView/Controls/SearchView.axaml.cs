using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using meteor.UI.Features.SearchView.ViewModels;
using Vector = Avalonia.Vector;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using Color = Avalonia.Media.Color;
using Point = Avalonia.Point;
using Size = Avalonia.Size;

namespace meteor.UI.Features.SearchView.Controls;

public class SearchView : UserControl
{
    private const int MaxItemsPerSearch = 10_000;
    private readonly IFileService _fileService;
    private readonly double _indentWidth = 20;
    private readonly double _itemHeight = 24;
    private readonly double _leftPadding = 10;
    private readonly double _rightPadding = 10;
    private readonly IThemeManager _themeManager;
    private Canvas _canvas;
    private Theme _currentTheme;
    private Grid _mainGrid;
    private ScrollViewer _scrollViewer;
    private TextBox _searchBox;
    private object _selectedItem;
    private SearchViewModel _viewModel;

    public SearchView(IThemeManager themeManager, IFileService fileService)
    {
        _themeManager = themeManager;
        _fileService = fileService;
        _currentTheme = _themeManager.CurrentTheme;
        _themeManager.ThemeChanged += OnThemeChanged;

        InitializeComponent();
        UpdateCanvasSize();
        Focus();
    }

    private void InitializeComponent()
    {
        _viewModel = new SearchViewModel(_fileService);
        DataContext = _viewModel;

        CreateMainGrid();
        CreateSearchBox();
        CreateScrollViewer();
        SetupEventHandlers();
        SetupLayout();
    }

    private void CreateMainGrid()
    {
        _mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Margin = new Thickness(10)
        };
    }

    private void CreateSearchBox()
    {
        _searchBox = new TextBox
        {
            Watermark = "Search in files...",
            Margin = new Thickness(0, 0, 0, 10),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse(_currentTheme.BorderBrush)),
            Background = new SolidColorBrush(Color.Parse(_currentTheme.BackgroundColor)),
            Foreground = new SolidColorBrush(Color.Parse(_currentTheme.TextColor)),
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(4)
        };
        _searchBox.KeyUp += OnSearchBoxKeyUp;
        Grid.SetRow(_searchBox, 0);
        _mainGrid.Children.Add(_searchBox);
    }

    private void CreateScrollViewer()
    {
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        _canvas = new Canvas();
        _scrollViewer.Content = _canvas;
        Grid.SetRow(_scrollViewer, 1);
        _mainGrid.Children.Add(_scrollViewer);
    }

    private void SetupEventHandlers()
    {
        _scrollViewer.ScrollChanged += OnScrollChanged;
        PointerPressed += OnPointerPressed;
        KeyDown += OnKeyDown;
    }

    private void SetupLayout()
    {
        Content = _mainGrid;
        Focusable = true;
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private void OnThemeChanged(object sender, Theme newTheme)
    {
        _currentTheme = newTheme;
        InvalidateVisual();
    }

    private void OnSearchBoxKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.ExecuteSearchCommand.Execute(_searchBox.Text);
            UpdateCanvasSize();
            InvalidateVisual();
        }
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty) UpdateCanvasSize();
    }

    private void UpdateCanvasSize()
    {
        var totalHeight = Math.Max(CalculateTotalHeight(_viewModel.SearchResults) + _itemHeight,
            _scrollViewer.Bounds.Height);
        var maxWidth = Math.Max(CalculateMaxWidth(_viewModel.SearchResults) + 20, _scrollViewer.Bounds.Width);
        _canvas.Width = maxWidth;
        _canvas.Height = totalHeight;
    }

    private double CalculateMaxWidth(ObservableCollection<object> items)
    {
        if (items.Count == 0) return 0;

        return items.Max(item =>
        {
            var filePath = (item as dynamic).FilePath;
            var matchCount = (item as dynamic).MatchCount;
            return _leftPadding + MeasureTextWidth(filePath) + _rightPadding +
                   MeasureTextWidth($"Matches: {matchCount}");
        });
    }

    private double MeasureTextWidth(string text)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("San Francisco"),
            13,
            new SolidColorBrush(Color.Parse(_currentTheme.TextColor))).Width;
    }

    private double CalculateTotalHeight(ObservableCollection<object> items)
    {
        return items.Count * _itemHeight;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var viewportRect = new Rect(new Point(0, 0),
            new Size(_scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height + 50));
        context.FillRectangle(new SolidColorBrush(Color.Parse(_currentTheme.BackgroundColor)), viewportRect);

        RenderItems(context, _viewModel.SearchResults, -_scrollViewer.Offset.Y, viewportRect);
    }

    private void RenderItems(DrawingContext context, ObservableCollection<object> items, double y, Rect viewport)
    {
        foreach (var item in items)
        {
            if (y + _itemHeight > 0 && y < viewport.Height) RenderItem(context, item, y, viewport);

            y += _itemHeight;

            if (y > viewport.Height) break;
        }
    }

    private void RenderItem(DrawingContext context, object item, double y, Rect viewport)
    {
        RenderItemBackground(context, item, y, viewport);
        RenderItemIcon(context, y);
        RenderItemText(context, item, y);
        RenderMatchCount(context, item, y);
    }

    private void RenderItemBackground(DrawingContext context, object item, double y, Rect viewport)
    {
        if (item == _selectedItem)
            context.FillRectangle(
                new SolidColorBrush(Color.Parse(_currentTheme.FileExplorerSelectedItemBackgroundColor)),
                new Rect(0, y, viewport.Width, _itemHeight));
    }

    private void RenderItemIcon(DrawingContext context, double y)
    {
        var iconSize = 16;
        var iconChar = "\uf15b"; // file icon
        var iconBrush = new SolidColorBrush(Color.Parse(_currentTheme.FileExplorerFileIconColor));
        var fontAwesomeSolid =
            new FontFamily(
                "avares://meteor.UI/Common/Assets/Fonts/FontAwesome/Font Awesome 6 Free-Solid-900.otf#Font Awesome 6 Free");
        var typeface = new Typeface(fontAwesomeSolid);

        var iconGeometry = CreateFormattedTextGeometry(iconChar, typeface, iconSize, iconBrush);

        var iconX = _leftPadding * 2.35 - _scrollViewer.Offset.X;
        var iconY = y + (_itemHeight - iconSize) / 2;

        iconGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(iconX + 1, iconY));
        context.DrawGeometry(iconBrush, null, iconGeometry);
    }

    private void RenderItemText(DrawingContext context, object item, double y)
    {
        var filePath = (item as dynamic).FilePath;
        var textBrush = new SolidColorBrush(Color.Parse(_currentTheme.TextColor));
        var textSize = 13;
        var typeface = new Typeface("San Francisco");

        var formattedText = new FormattedText(
            filePath,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            textSize,
            textBrush
        );

        var textX = _leftPadding * 2.65 + 20 - _scrollViewer.Offset.X;
        var textY = y + (_itemHeight - formattedText.Height) / 2;

        context.DrawText(formattedText, new Point(textX, textY));
    }

    private void RenderMatchCount(DrawingContext context, object item, double y)
    {
        var matchCount = (item as dynamic).MatchCount;
        var textBrush = new SolidColorBrush(Color.Parse(_currentTheme.TextColor));
        var textSize = 11;
        var typeface = new Typeface("San Francisco");

        var formattedText = new FormattedText(
            $"Matches: {matchCount}",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            textSize,
            textBrush
        );

        var textX = _scrollViewer.Viewport.Width - formattedText.Width - _rightPadding - _scrollViewer.Offset.X;
        var textY = y + (_itemHeight - formattedText.Height) / 2;

        context.DrawText(formattedText, new Point(textX, textY));
    }

    private Geometry CreateFormattedTextGeometry(string text, Typeface typeface, double size, IBrush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            brush
        ).BuildGeometry(new Point(0, 0));
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var itemClicked = FindClickedItem(_viewModel.SearchResults, point.Y, -_scrollViewer.Offset.Y);

        if (itemClicked != null)
        {
            _selectedItem = itemClicked;
            OnFileSelected((itemClicked as dynamic).FilePath);
            InvalidateVisual();
        }
    }

    private object FindClickedItem(ObservableCollection<object> items, double clickY, double startY)
    {
        foreach (var item in items)
        {
            if (IsClickWithinItemBounds(clickY, startY))
                return item;

            startY += _itemHeight;

            if (startY > clickY) break;
        }

        return null;
    }

    private bool IsClickWithinItemBounds(double clickY, double startY)
    {
        return clickY >= startY && clickY < startY + _itemHeight;
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
                ActivateSelectedItem();
                break;
        }

        e.Handled = true;
    }

    private void MoveSelection(int direction)
    {
        var allItems = _viewModel.SearchResults.ToList();
        var currentIndex = allItems.IndexOf(_selectedItem);
        var newIndex = (currentIndex + direction + allItems.Count) % allItems.Count;
        _selectedItem = allItems[newIndex];
        ScrollToItem(_selectedItem);
        InvalidateVisual();
    }

    private void ActivateSelectedItem()
    {
        if (_selectedItem != null) OnFileSelected((_selectedItem as dynamic).FilePath);
    }

    private void ScrollToItem(object item)
    {
        var allItems = _viewModel.SearchResults.ToList();
        var index = allItems.IndexOf(item);
        var itemTop = index * _itemHeight;
        var itemBottom = itemTop + _itemHeight;

        var viewportTop = _scrollViewer.Offset.Y;
        var viewportBottom = viewportTop + _scrollViewer.Viewport.Height;

        var desiredOffset = CalculateDesiredOffset(itemTop, itemBottom, viewportTop, viewportBottom);

        if (desiredOffset != _scrollViewer.Offset.Y) UpdateScrollViewerOffset(desiredOffset);
    }

    private double CalculateDesiredOffset(double itemTop, double itemBottom, double viewportTop,
        double viewportBottom)
    {
        if (itemTop < viewportTop)
            return itemTop;
        if (itemBottom > viewportBottom)
            return itemBottom - _scrollViewer.Viewport.Height;
        return _scrollViewer.Offset.Y;
    }

    private void UpdateScrollViewerOffset(double desiredOffset)
    {
        var maxScrollOffset = Math.Max(0, _canvas.Bounds.Height - _scrollViewer.Viewport.Height);
        desiredOffset = Math.Max(0, Math.Min(desiredOffset, maxScrollOffset));
        _scrollViewer.Offset = new Vector(0, desiredOffset);
    }

    public event EventHandler<string> FileSelected;

    private void OnFileSelected(string filePath)
    {
        FileSelected?.Invoke(this, filePath);
    }
}