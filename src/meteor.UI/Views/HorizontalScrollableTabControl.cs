using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace meteor.UI.Views;

public class HorizontalScrollableTabControl : TabControl
{
    private ScrollViewer? _scrollViewer;

    public HorizontalScrollableTabControl()
    {
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Bubble);
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

            // Check if the cursor is within the bounds of the ScrollViewer
            if (bounds.Contains(point))
            {
                _scrollViewer.Offset = new Vector(
                    _scrollViewer.Offset.X - e.Delta.Y * 50,
                    _scrollViewer.Offset.Y);
                e.Handled = true;
            }
        }
    }
}