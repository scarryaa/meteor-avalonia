using System.Collections;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Timer = System.Timers.Timer;

namespace meteor.UI.Controls;

public class HorizontalScrollableTabControl : Avalonia.Controls.TabControl
{
    private ScrollViewer? _scrollViewer;
    private TabItem? _draggedItem;
    private int _draggedIndex;
    private TabItem? _previewItem;
    private int _previewIndex;
    private readonly Dictionary<TabItem, (IBrush? BorderBrush, Thickness BorderThickness)> _originalBorders = new();
    private readonly Timer? _autoScrollTimer;
    private const double AutoScrollInterval = 3;
    private const double AutoScrollThreshold = 20;
    private const double AutoScrollStep = 1;
    private Point _lastKnownMousePosition;
    private bool _shouldCommitDrag;

    public static readonly StyledProperty<IBrush> PreviewBorderBrushProperty =
        AvaloniaProperty.Register<HorizontalScrollableTabControl, IBrush>(
            nameof(PreviewBorderBrush), new SolidColorBrush(Colors.Black));

    public IBrush PreviewBorderBrush
    {
        get => GetValue(PreviewBorderBrushProperty);
        set => SetValue(PreviewBorderBrushProperty, value);
    }

    public HorizontalScrollableTabControl()
    {
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Bubble);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        _autoScrollTimer = new Timer(AutoScrollInterval);
        _autoScrollTimer.Elapsed += OnAutoScrollTimerElapsed;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelDragDrop();
            e.Handled = true;
        }
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_scrollViewer != null && e.KeyModifiers is KeyModifiers.Shift or KeyModifiers.None)
        {
            var point = e.GetPosition(_scrollViewer);
            var bounds = _scrollViewer.Bounds;
            if (bounds.Contains(point))
            {
                _scrollViewer.Offset = new Vector(
                    _scrollViewer.Offset.X - e.Delta.Y * 50,
                    _scrollViewer.Offset.Y);
                e.Handled = true;
            }
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_scrollViewer == null) return;
        var point = e.GetPosition(_scrollViewer);
        _draggedItem = GetTabItemAt(point);
        if (_draggedItem != null)
        {
            _draggedIndex = ItemContainerGenerator.IndexFromContainer(_draggedItem);
            _autoScrollTimer?.Start();
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_scrollViewer == null || _draggedItem == null ||
            !e.GetCurrentPoint(_scrollViewer).Properties.IsLeftButtonPressed)
            return;

        var point = e.GetPosition(_scrollViewer);
        _lastKnownMousePosition = point;
        var targetItem = GetTabItemAt(point);

        if (targetItem != null)
        {
            var targetIndex = ItemContainerGenerator.IndexFromContainer(targetItem);
            UpdatePreviewDrop(targetIndex);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_previewItem != null && _shouldCommitDrag)
        {
            CommitDrop();
            ResetPreviewItem(_previewItem);
        }

        CancelDragDrop();
        _shouldCommitDrag = false;
    }

    private void CancelDragDrop()
    {
        if (_previewItem != null) ResetPreviewItem(_previewItem);

        _draggedItem = null;
        _draggedIndex = -1;
        _previewItem = null;
        _previewIndex = -1;

        _autoScrollTimer?.Stop();
    }

    private TabItem? GetTabItemAt(Point point)
    {
        return this.GetVisualDescendants()
            .OfType<TabItem>()
            .FirstOrDefault(item =>
            {
                var itemBounds = item.Bounds;
                itemBounds = itemBounds.Translate(new Vector(-_scrollViewer!.Offset.X, -_scrollViewer.Offset.Y));
                return itemBounds.Contains(point);
            });
    }

    private void UpdatePreviewDrop(int newIndex)
    {
        if (newIndex == _draggedIndex)
        {
            if (_previewItem != null) ResetPreviewItem(_previewItem);
            _previewItem = null;
            _previewIndex = -1;
            _shouldCommitDrag = false;
            return;
        }

        _shouldCommitDrag = true;

        if (newIndex != _previewIndex)
        {
            if (_previewItem != null) ResetPreviewItem(_previewItem);

            _previewItem = (TabItem)ItemContainerGenerator.ContainerFromIndex(newIndex);
            _previewIndex = newIndex;

            if (_previewItem != null)
            {
                var mainBorder = _previewItem.GetVisualDescendants().OfType<Border>()
                    .FirstOrDefault(b => b.Name == "MainBorder");
                if (mainBorder != null)
                {
                    // Store the original values
                    _originalBorders[_previewItem] = (mainBorder.BorderBrush, mainBorder.BorderThickness);

                    mainBorder.BorderBrush = PreviewBorderBrush;
                    mainBorder.BorderThickness = new Thickness(0, 0, 1, 0);
                }
            }
        }
    }

    private void ResetPreviewItem(TabItem item)
    {
        if (_originalBorders.TryGetValue(item, out var originalValues))
        {
            var mainBorder = item.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "MainBorder");
            if (mainBorder != null)
            {
                mainBorder.BorderBrush = originalValues.BorderBrush;
                mainBorder.BorderThickness = originalValues.BorderThickness;
            }

            _originalBorders.Remove(item);
        }
    }

    private void CommitDrop()
    {
        if (_previewItem != null && _draggedItem != null) MoveItem(_draggedIndex, _previewIndex);
    }

    private void MoveItem(int oldIndex, int newIndex)
    {
        var selectedItem = SelectedItem;
        if (ItemsSource != null)
        {
            if (ItemsSource is IList list)
            {
                var item = list[oldIndex];
                list.RemoveAt(oldIndex);
                list.Insert(newIndex, item);
            }
            else
            {
                return;
            }
        }
        else if (Items != null)
        {
            var item = Items[oldIndex];
            Items.RemoveAt(oldIndex);
            Items.Insert(newIndex, item);
        }

        // Restore the selected item
        SelectedItem = selectedItem;
    }

    private void OnAutoScrollTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_scrollViewer == null || _draggedItem == null) return;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var point = _lastKnownMousePosition;
            var scrollViewerBounds = _scrollViewer.Bounds;

            if (point.X < scrollViewerBounds.Left + AutoScrollThreshold)
                // Scroll left
                _scrollViewer.Offset = new Vector(
                    _scrollViewer.Offset.X - AutoScrollStep,
                    _scrollViewer.Offset.Y);
            else if (point.X > scrollViewerBounds.Right - AutoScrollThreshold)
                // Scroll right
                _scrollViewer.Offset = new Vector(
                    _scrollViewer.Offset.X + AutoScrollStep,
                    _scrollViewer.Offset.Y);
        });
    }
}