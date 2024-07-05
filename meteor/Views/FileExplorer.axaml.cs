using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using meteor.Models;
using meteor.ViewModels;

namespace meteor.Views;

public partial class FileExplorer : UserControl
{
    private const int DoubleClickThreshold = 250;

    public static readonly StyledProperty<ObservableCollection<File>> ItemsProperty =
        AvaloniaProperty.Register<FileExplorer, ObservableCollection<File>>(nameof(Items));

    public static readonly StyledProperty<FileExplorerViewModel> ViewModelProperty =
        AvaloniaProperty.Register<FileExplorer, FileExplorerViewModel>(nameof(ViewModel));

    public static readonly StyledProperty<File> SelectedItemProperty =
        AvaloniaProperty.Register<FileExplorer, File>(nameof(SelectedItem));

    private DateTime _lastClickTime = DateTime.MinValue;
    private double _totalHeight;

    public FileExplorer()
    {
        InitializeComponent();
        Focusable = true;

        DataContextChanged += OnDataContextChanged;
        PointerPressed += OnPointerPressed;
        KeyDown += OnKeyDown;
    }

    public FileExplorerViewModel ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public ObservableCollection<File> Items
    {
        get => GetValue(ItemsProperty) ?? [];
        set => SetValue(ItemsProperty, value);
    }

    public File SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public event EventHandler<string> FileClicked;
    public event EventHandler<string> FileDoubleClicked;

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is FileExplorerViewModel viewModel)
        {
            ViewModel = viewModel;
            Items = viewModel.Items;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.InvalidateRequired += (_, _) => InvalidateVisual();
        }
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Items))
        {
            InvalidateVisual();
            InvalidateMeasure();
            InvalidateArrange();
        }
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var clickedItem = GetItemAtPoint(point, Items, 0, 0);
        if (clickedItem != null)
        {
            SelectedItem = clickedItem;
            var now = DateTime.Now;

            if ((now - _lastClickTime).TotalMilliseconds <= DoubleClickThreshold)
            {
                if (!clickedItem.IsDirectory) FileDoubleClicked?.Invoke(this, clickedItem.Path);
            }
            else
            {
                _lastClickTime = now;

                if (!clickedItem.IsDirectory) FileClicked?.Invoke(this, clickedItem.Path);
                if (clickedItem.IsDirectory)
                {
                    clickedItem.IsExpanded = !clickedItem.IsExpanded;
                    InvalidateMeasure();
                    InvalidateArrange();
                }
            }
        }

        InvalidateVisual();
    }

    private File GetItemAtPoint(Point point, ObservableCollection<File> items, double x, double y)
    {
        foreach (var item in items)
        {
            if (point.Y >= y && point.Y <= y + 20 && point.X >= 0 && point.X <= Bounds.Width)
                return item;

            y += 20;

            if (item.IsExpanded)
            {
                var subItem = GetItemAtPoint(point, item.Items, x + 20, y);
                if (subItem != null)
                    return subItem;

                y += MeasureSubItemsHeight(item.Items, x + 20);
            }
        }

        return null;
    }

    private double MeasureSubItemsHeight(ObservableCollection<File> items, double x)
    {
        return items.Sum(item => 20 + (item.IsExpanded ? MeasureSubItemsHeight(item.Items, x + 20) : 0));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _totalHeight = MeasureItems(Items, 10);
        return new Size(availableSize.Width, _totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ArrangeItems(Items, 10, 0);
        return new Size(finalSize.Width, _totalHeight);
    }

    private double MeasureItems(ObservableCollection<File> items, double x)
    {
        return items.Sum(item =>
        {
            var formattedText = new FormattedText(item.Name, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                Typeface.Default, FontSize, Brushes.Black);

            return 20 + (item.IsExpanded ? MeasureItems(item.Items, x + 20) : 0);
        });
    }

    private double ArrangeItems(ObservableCollection<File> items, double x, double y)
    {
        foreach (var item in items)
        {
            y += 20;
            if (item.IsExpanded)
                y = ArrangeItems(item.Items, x + 20, y);
        }
        return y;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        RenderItems(context, Items, 10, 0);
    }

    private double RenderItems(DrawingContext context, ObservableCollection<File> items, double x, double y)
    {
        foreach (var item in items)
        {
            y = RenderItem(context, item, x, y);
            if (item.IsExpanded)
                y = RenderItems(context, item.Items, x + 10, y);
        }
        return y;
    }

    private double RenderItem(DrawingContext context, File item, double x, double y)
    {
        var iconBrush = ViewModel.IconBrush;
        var backgroundBrush = item == SelectedItem ? ViewModel.SelectedBrush : Brushes.Transparent;

        context.FillRectangle(backgroundBrush, new Rect(0, y, Bounds.Width, 20));

        var iconText = item.IsDirectory ? "\uf07b" : "\uf15b";
        var formattedIcon = new FormattedText(iconText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("FontAwesome"), FontSize, iconBrush);

        var textGeometry = formattedIcon.BuildGeometry(new Point(x, y + 3));

        if (item.IsExpanded || !item.IsDirectory)
        {
            context.DrawGeometry(null, new Pen(ViewModel.OutlineBrush), textGeometry);
            context.DrawGeometry(iconBrush, null, textGeometry);
        }
        else
        {
            context.DrawGeometry(null, new Pen(ViewModel.OutlineBrush), textGeometry);
        }

        var formattedName = new FormattedText(item.Name, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily), FontSize, Foreground);
        context.DrawText(formattedName, new Point(x + 20, y + 2));

        return y + 20;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel == null || Items == null || Items.Count == 0)
            return;

        switch (e.Key)
        {
            case Key.Up:
            case Key.Left:
                NavigateUpOrLeft(e);
                break;
            case Key.Down:
            case Key.Right:
                NavigateDownOrRight(e);
                break;
            case Key.Enter:
                OpenSelectedItem();
                break;
        }

        e.Handled = true;
        InvalidateVisual();
    }

    private void NavigateUpOrLeft(KeyEventArgs e)
    {
        if (e.Key == Key.Left && SelectedItem?.IsDirectory == true && SelectedItem.IsExpanded)
            // Collapse the directory if it's expanded
            SelectedItem.IsExpanded = false;
        else
            // Move selection up
            SelectAdjacentItem(-1);
    }

    private void NavigateDownOrRight(KeyEventArgs e)
    {
        if (e.Key == Key.Right && SelectedItem?.IsDirectory == true && !SelectedItem.IsExpanded)
            // Expand the directory if it's collapsed
            SelectedItem.IsExpanded = true;
        else
            // Move selection down
            SelectAdjacentItem(1);
    }

    private void SelectAdjacentItem(int direction)
    {
        var flatList = FlattenItems(Items);
        var index = flatList.IndexOf(SelectedItem);
        var newIndex = index + direction;
        if (newIndex >= 0 && newIndex < flatList.Count)
            SelectedItem = flatList[newIndex];
    }

    private void OpenSelectedItem()
    {
        if (SelectedItem != null)
        {
            if (SelectedItem.IsDirectory)
                SelectedItem.IsExpanded = !SelectedItem.IsExpanded;
            else
                // Open the file
                FileDoubleClicked?.Invoke(this, SelectedItem.Path);
        }
    }

    private File FindParentItem(ObservableCollection<File> items, File target)
    {
        foreach (var item in items)
        {
            if (item.Items.Contains(target))
                return item;

            var result = FindParentItem(item.Items, target);
            if (result != null)
                return result;
        }

        return null;
    }
    
    private List<File> FlattenItems(ObservableCollection<File> items)
    {
        return items.SelectMany(item =>
            new[] { item }.Concat(item.IsDirectory && item.IsExpanded
                ? FlattenItems(item.Items)
                : Enumerable.Empty<File>())
        ).ToList();
    }
}