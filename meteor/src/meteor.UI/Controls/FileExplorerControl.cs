using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using meteor.Core.Models;
using Color = Avalonia.Media.Color;
using Point = Avalonia.Point;
using Size = Avalonia.Size;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;

namespace meteor.UI.Controls;

public class FileExplorerControl : UserControl
{
    private const int MaxItemsPerDirectory = 10_000;
    private readonly ObservableCollection<FileItem> _items;
    private readonly double _itemHeight = 24;
    private readonly double _indentWidth = 20;
    private readonly double _leftPadding = 10;
    private readonly double _rightPadding = 10;
    private ScrollViewer _scrollViewer;
    private Canvas _canvas;
    private FileItem _selectedItem;
    private Button _selectPathButton;
    private Grid _mainGrid;

    public FileExplorerControl()
    {
        _items = new ObservableCollection<FileItem>();
        InitializeComponent();
        UpdateCanvasSize();
        Focus();
    }

    private void InitializeComponent()
    {
        _mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        _selectPathButton = new Button
        {
            Content = "Select Folder",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 10),
            Cursor = new Cursor(StandardCursorType.Hand),
            Classes = { "noBg" }
        };

        // Create and apply button styles
        _selectPathButton.Styles.Add(CreateButtonStyles());

        _selectPathButton.Click += OnSelectPathButtonClick;

        Grid.SetRow(_selectPathButton, 0);
        _mainGrid.Children.Add(_selectPathButton);

        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        _canvas = new Canvas();
        _scrollViewer.Content = _canvas;
        Grid.SetRow(_scrollViewer, 1);
        _mainGrid.Children.Add(_scrollViewer);

        Content = _mainGrid;

        _scrollViewer.ScrollChanged += OnScrollChanged;
        PointerPressed += OnPointerPressed;
        KeyDown += OnKeyDown;
        Focusable = true;

        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        UpdateSelectPathButtonVisibility();
    }

    private Styles CreateButtonStyles()
    {
        var styles = new Styles
        {
            // Normal state
            new Style(x => x.OfType<Button>().Class("noBg"))
            {
                Setters =
                {
                    new Setter(TemplateProperty, CreateButtonTemplate())
                }
            },
            // PointerOver state
            new Style(x => x.OfType<Button>().Class("noBg").Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(TemplateProperty, CreateButtonTemplate(true))
                }
            },
            // Pressed state
            new Style(x => x.OfType<Button>().Class("noBg").Class(":pressed"))
            {
                Setters =
                {
                    new Setter(TemplateProperty, CreateButtonTemplate(isPressed: true))
                }
            },
            // Disabled state
            new Style(x => x.OfType<Button>().Class("noBg").Class(":disabled"))
            {
                Setters =
                {
                    new Setter(TemplateProperty, CreateButtonTemplate(isDisabled: true))
                }
            }
        };

        return styles;
    }

    private IControlTemplate CreateButtonTemplate(bool isPointerOver = false, bool isPressed = false,
        bool isDisabled = false)
    {
        return new FuncControlTemplate((parent, scope) =>
        {
            var contentPresenter = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                Background = isPointerOver
                    ? new SolidColorBrush(Color.Parse("#F0F0F0"))
                    : isPressed
                        ? new SolidColorBrush(Color.Parse("#E8E8E8"))
                        : new SolidColorBrush(Color.Parse("#E0E0E0")),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(4),
                [!ContentPresenter.ContentProperty] = parent[!ContentProperty],
                [!ContentPresenter.ContentTemplateProperty] = parent[!ContentTemplateProperty]
            };

            if (isDisabled)
                contentPresenter.Foreground = new SolidColorBrush(Color.Parse("#B0B0B0"));
            else
                contentPresenter.Foreground = new SolidColorBrush(Color.Parse("#202020"));

            return contentPresenter;
        });
    }

    private void UpdateSelectPathButtonVisibility()
    {
        _selectPathButton.IsVisible = _items.Count == 0;
    }

    private async void OnSelectPathButtonClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        var window = GetMainWindow();
        var result = await dialog.ShowAsync(window);

        if (!string.IsNullOrEmpty(result))
        {
            _items.Clear();
            _items.Add(new FileItem(result, true));
            PopulateChildren(_items[0]);
            _items[0].IsExpanded = true;
            UpdateCanvasSize();
            InvalidateVisual();
            UpdateSelectPathButtonVisibility();
        }
    }

    private Window GetMainWindow()
    {
        return (Window)VisualRoot;
    }

    private void PopulateChildren(FileItem item)
    {
        if (item.ChildrenPopulated) return;

        try
        {
            var children = new List<FileItem>();
            var directories = Directory.GetDirectories(item.FullPath)
                .Select(dir => new FileItem(dir, true))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase);
            var files = Directory.GetFiles(item.FullPath)
                .Select(file => new FileItem(file, false))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase);

            children.AddRange(directories);
            children.AddRange(files);

            if (children.Count > MaxItemsPerDirectory)
            {
                children = children.Take(MaxItemsPerDirectory).ToList();
                children.Add(new FileItem("... (More items not shown)", item.FullPath, false));
            }

            item.Children.AddRange(children);
            item.ChildrenPopulated = true;
        }
        catch (Exception ex)
        {
            item.Children.Add(new FileItem($"Error: {ex.Message}", item.FullPath, false));
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty) UpdateCanvasSize();
    }

    private void UpdateCanvasSize()
    {
        var totalHeight = Math.Max(CalculateTotalHeight(_items) + _itemHeight, _scrollViewer.Bounds.Height);
        var maxWidth = Math.Max(CalculateMaxWidth(_items), _scrollViewer.Bounds.Width);
        _canvas.Width = maxWidth;
        _canvas.Height = totalHeight;
    }

    private double CalculateMaxWidth(IEnumerable<FileItem> items, int depth = 0)
    {
        var itemsList = items.ToList();
        if (itemsList.Count == 0) return 0;

        return itemsList.Max(item =>
        {
            var itemWidth = depth * _indentWidth + _leftPadding + MeasureTextWidth(item.Name) + _rightPadding;
            if (item.IsDirectory && item.IsExpanded)
                itemWidth = Math.Max(itemWidth, CalculateMaxWidth(item.Children, depth + 1));
            return itemWidth;
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
            Brushes.Black).Width + _rightPadding + _leftPadding;
    }

    private double CalculateTotalHeight(IEnumerable<FileItem> items)
    {
        return items.Sum(item => _itemHeight + (item.IsExpanded ? CalculateTotalHeight(item.Children) : 0));
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var viewportRect = new Rect(new Point(0, 0),
            new Size(_scrollViewer.Viewport.Width, _scrollViewer.Viewport.Height + 50));
        context.FillRectangle(Brushes.White, viewportRect);

        var buttonHeight = _selectPathButton.IsVisible ? _selectPathButton.Bounds.Height : 0;
        RenderItems(context, _items, 0, -_scrollViewer.Offset.Y + buttonHeight, viewportRect);
    }

    private double RenderItems(DrawingContext context, IEnumerable<FileItem> items, int indentLevel, double y,
        Rect viewport)
    {
        foreach (var item in items)
        {
            if (y + _itemHeight > 0 && y < viewport.Height) RenderItem(context, item, indentLevel, y, viewport);

            y += _itemHeight;

            if (item.IsExpanded) y = RenderItems(context, item.Children, indentLevel + 1, y, viewport);

            if (y > viewport.Height) break;
        }

        return y;
    }

    private void RenderItem(DrawingContext context, FileItem item, int indentLevel, double y, Rect viewport)
    {
        if (item == _selectedItem)
            context.FillRectangle(new SolidColorBrush(Color.FromUInt32(0x648BCDCD)),
                new Rect(0, y, viewport.Width, _itemHeight));

        var brush = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        var text = new FormattedText(item.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("San Francisco"), 13, brush);

        var iconSize = 16;
        var iconChar = item.IsDirectory ? "\uf07b" : "\uf15b"; // folder icon : file icon
        var iconBrush = new SolidColorBrush(Color.FromRgb(155, 155, 155));
        var typeface = new Typeface("Font Awesome 6 Free", FontStyle.Normal, FontWeight.Black);

        var iconGeometry = new FormattedText(
            iconChar,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            iconSize,
            iconBrush
        ).BuildGeometry(new Point(0, 0));

        var iconX = _leftPadding * 2.35 + indentLevel * _indentWidth - _scrollViewer.Offset.X;
        var iconY = y + (_itemHeight - iconSize) / 2;

        iconGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(iconX + 1, iconY));
        context.DrawGeometry(iconBrush, null, iconGeometry);

        var chevronBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
        if (item.IsDirectory)
        {
            var chevronChar = item.IsExpanded ? "\uf078" : "\uf054"; // chevron-down : chevron-right
            var chevronGeometry = new FormattedText(
                chevronChar,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                12,
                chevronBrush
            ).BuildGeometry(new Point(0, 0));

            var chevronX = iconX - iconSize;
            var chevronY = y + (_itemHeight - 12) / 2;

            chevronGeometry.Transform = new MatrixTransform(Matrix.CreateTranslation(chevronX, chevronY));
            context.DrawGeometry(chevronBrush, null, chevronGeometry);
        }

        var textVerticalOffset = (_itemHeight - text.Height) / 2;
        context.DrawText(text,
            new Point(_leftPadding * 2.65 + indentLevel * _indentWidth + iconSize + 4 - _scrollViewer.Offset.X,
                y + textVerticalOffset));
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var itemClicked = FindClickedItem(_items, point, 0, -_scrollViewer.Offset.Y);

        if (itemClicked != null)
        {
            _selectedItem = itemClicked;
            if (itemClicked.IsDirectory)
            {
                itemClicked.IsExpanded = !itemClicked.IsExpanded;
                if (itemClicked.IsExpanded && !itemClicked.ChildrenPopulated) PopulateChildren(itemClicked);
            }

            UpdateCanvasSize();
            InvalidateVisual();
        }
    }

    private FileItem FindClickedItem(IEnumerable<FileItem> items, Point point, int indentLevel, double y)
    {
        foreach (var item in items)
        {
            if (point.Y >= y && point.Y < y + _itemHeight &&
                point.X >= _leftPadding + indentLevel * _indentWidth - _scrollViewer.Offset.X &&
                point.X <= _canvas.Width - _rightPadding)
                return item;

            y += _itemHeight;

            if (item.IsExpanded)
            {
                var childResult = FindClickedItem(item.Children, point, indentLevel + 1, y);
                if (childResult != null) return childResult;
                y += CalculateTotalHeight(item.Children);
            }

            if (y > point.Y) break;
        }

        return null;
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
            case Key.Left:
                CollapseOrMoveUp();
                break;
            case Key.Right:
                ExpandOrMoveDown();
                break;
            case Key.Enter:
                ActivateSelectedItem();
                break;
        }

        e.Handled = true;
    }

    private void MoveSelection(int direction)
    {
        var allItems = GetFlattenedItems(_items);
        var currentIndex = allItems.IndexOf(_selectedItem);
        var newIndex = (currentIndex + direction + allItems.Count) % allItems.Count;
        _selectedItem = allItems[newIndex];
        ScrollToItem(_selectedItem);
        InvalidateVisual();
    }

    private void CollapseOrMoveUp()
    {
        if (_selectedItem?.IsDirectory == true && _selectedItem.IsExpanded)
        {
            _selectedItem.IsExpanded = false;
            UpdateCanvasSize();
        }
        else
        {
            var parent = FindParentItem(_items, _selectedItem);
            if (parent != null)
            {
                _selectedItem = parent;
                ScrollToItem(_selectedItem);
            }
        }

        InvalidateVisual();
    }

    private void ExpandOrMoveDown()
    {
        if (_selectedItem?.IsDirectory == true && !_selectedItem.IsExpanded)
        {
            _selectedItem.IsExpanded = true;
            if (!_selectedItem.ChildrenPopulated) PopulateChildren(_selectedItem);
            UpdateCanvasSize();
        }
        else if (_selectedItem?.IsDirectory == true && _selectedItem.IsExpanded && _selectedItem.Children.Any())
        {
            _selectedItem = _selectedItem.Children.First();
            ScrollToItem(_selectedItem);
        }

        InvalidateVisual();
    }

    private void ActivateSelectedItem()
    {
        if (_selectedItem?.IsDirectory == true)
        {
            _selectedItem.IsExpanded = !_selectedItem.IsExpanded;
            if (_selectedItem.IsExpanded && !_selectedItem.ChildrenPopulated) PopulateChildren(_selectedItem);
            UpdateCanvasSize();
        }

        InvalidateVisual();
    }

    private List<FileItem> GetFlattenedItems(IEnumerable<FileItem> items)
    {
        var result = new List<FileItem>();
        foreach (var item in items)
        {
            result.Add(item);
            if (item.IsExpanded) result.AddRange(GetFlattenedItems(item.Children));
        }

        return result;
    }

    private FileItem FindParentItem(IEnumerable<FileItem> items, FileItem target)
    {
        foreach (var item in items)
        {
            if (item.Children.Contains(target)) return item;
            if (item.IsExpanded)
            {
                var result = FindParentItem(item.Children, target);
                if (result != null) return result;
            }
        }

        return null;
    }

    private async void ScrollToItem(FileItem item)
    {
        var allItems = GetFlattenedItems(_items);
        var index = allItems.IndexOf(item);
        var itemTop = index * _itemHeight;
        var itemBottom = itemTop + _itemHeight;

        var viewportTop = _scrollViewer.Offset.Y;
        var viewportBottom = viewportTop + _scrollViewer.Viewport.Height;

        var desiredOffset = _scrollViewer.Offset.Y;

        if (itemTop < viewportTop)
            desiredOffset = itemTop;
        else if (itemBottom > viewportBottom)
            desiredOffset = itemBottom - _scrollViewer.Viewport.Height;
        else
            return;

        var maxScrollOffset = Math.Max(0, _canvas.Bounds.Height - _scrollViewer.Viewport.Height);
        desiredOffset = Math.Max(0, Math.Min(desiredOffset, maxScrollOffset));
        _scrollViewer.Offset = new Point(0, desiredOffset);
    }
}