using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
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

    public static readonly StyledProperty<IBrush> SelectedBrushProperty =
        AvaloniaProperty.Register<FileExplorer, IBrush>(nameof(SelectedBrush));

    public static readonly StyledProperty<IBrush> IconBrushProperty =
        AvaloniaProperty.Register<FileExplorer, IBrush>(nameof(IconBrush));

    public static readonly StyledProperty<IBrush> OutlineBrushProperty =
        AvaloniaProperty.Register<FileExplorer, IBrush>(nameof(OutlineBrush));

    private bool _isDoubleClick;
    private DateTime _lastClickTime = DateTime.MinValue;

    private double _totalHeight;

    public FileExplorer()
    {
        InitializeComponent();
        Focusable = true;

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

    public IBrush SelectedBrush
    {
        get => GetValue(SelectedBrushProperty);
        set => SetValue(SelectedBrushProperty, value);
    }

    public IBrush IconBrush
    {
        get => GetValue(IconBrushProperty);
        set => SetValue(IconBrushProperty, value);
    }

    public IBrush OutlineBrush
    {
        get => GetValue(OutlineBrushProperty);
        set => SetValue(OutlineBrushProperty, value);
    }

    public event EventHandler<string> FileClicked;
    public event EventHandler<string> FileDoubleClicked;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is FileExplorerViewModel viewModel)
        {
            ViewModel = viewModel;
            Items = viewModel.Items;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
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
                _isDoubleClick = true;
                if (!clickedItem.IsDirectory) FileDoubleClicked?.Invoke(this, clickedItem.Path);
            }
            else
            {
                _isDoubleClick = false;
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
            if (point.Y >= y && point.Y <= y + 20)
                if (point.X >= 0 && point.X <= Bounds.Width)
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
        double y = 0;

        foreach (var item in items)
        {
            y += 20;

            if (item.IsExpanded) y += MeasureSubItemsHeight(item.Items, x + 20);
        }

        return y;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double y = 0;
        _totalHeight = 0;

        foreach (var item in Items)
            y = MeasureItem(item, 10, y);

        _totalHeight = y;
        return new Size(availableSize.Width, _totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;

        foreach (var item in Items)
            y = ArrangeItem(item, 10, y);

        return new Size(finalSize.Width, _totalHeight);
    }

    private double MeasureItem(File item, double x, double y)
    {
        var formattedText = new FormattedText(item.Name, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            Typeface.Default, FontSize, Brushes.Black);
        var itemWidth = x + 20 + formattedText.Width;

        y += 20;

        if (item.IsExpanded)
            foreach (var subItem in item.Items)
                y = MeasureItem(subItem, x + 20, y);

        return y;
    }

    private double ArrangeItem(File item, double x, double y)
    {
        y += 20;

        if (item.IsExpanded)
            foreach (var subItem in item.Items)
                y = ArrangeItem(subItem, x + 20, y);

        return y;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double y = 0;

        foreach (var item in Items)
            y = RenderItem(context, item, 10, y);
    }

    private double RenderItem(DrawingContext context, File item, double x, double y)
    {
        var iconBrush = IconBrush;
        var backgroundBrush = item == SelectedItem ? SelectedBrush : Brushes.Transparent;

        context.FillRectangle(backgroundBrush, new Rect(0, y, Bounds.Width, 20));

        var formattedText = new FormattedText(
            item.IsDirectory ? "\uf07b" : "\uf15b",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("FontAwesome"),
            FontSize,
            iconBrush);

        var textGeometry = formattedText.BuildGeometry(new Point(x, y + 3));

        if (item.IsExpanded || !item.IsDirectory)
        {
            // Draw the outline and fill the icon
            context.DrawGeometry(null, new Pen(OutlineBrush), textGeometry);
            context.DrawGeometry(iconBrush, null, textGeometry);
        }
        else
        {
            // Draw only the outline of the icon
            context.DrawGeometry(null, new Pen(OutlineBrush), textGeometry);
        }

        context.DrawText(
            new FormattedText(
                item.Name,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily),
                FontSize,
                Brushes.Black),
            new Point(x + 20, y + 2));

        y += 20;

        if (item.IsExpanded)
            foreach (var subItem in item.Items)
                y = RenderItem(context, subItem, x + 10, y);

        return y;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel == null || Items == null || Items.Count == 0)
            return;

        if (e.Key == Key.Up)
            SelectPreviousItem();
        else if (e.Key == Key.Down)
            SelectNextItem();
        else if (e.Key == Key.Left)
            CollapseSelectedItem();
        else if (e.Key == Key.Right) ExpandSelectedItem();

        InvalidateVisual();
    }

    private void SelectPreviousItem()
    {
        var flatList = FlattenItems(Items);
        var index = flatList.IndexOf(SelectedItem);
        if (index > 0) SelectedItem = flatList[index - 1];
    }

    private void SelectNextItem()
    {
        var flatList = FlattenItems(Items);
        var index = flatList.IndexOf(SelectedItem);
        if (index < flatList.Count - 1) SelectedItem = flatList[index + 1];
    }

    private void CollapseSelectedItem()
    {
        if (SelectedItem != null && SelectedItem.IsDirectory && SelectedItem.IsExpanded)
            SelectedItem.IsExpanded = false;
    }

    private void ExpandSelectedItem()
    {
        if (SelectedItem != null && SelectedItem.IsDirectory && !SelectedItem.IsExpanded)
            SelectedItem.IsExpanded = true;
    }

    private List<File> FlattenItems(ObservableCollection<File> items)
    {
        var flatList = new List<File>();

        foreach (var item in items)
        {
            flatList.Add(item);
            if (item.IsDirectory && item.IsExpanded) flatList.AddRange(FlattenItems(item.Items));
        }

        return flatList;
    }
}