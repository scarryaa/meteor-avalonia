using Avalonia.Controls;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using meteor.UI.Controls;
using meteor.UI.Interfaces.Services.Editor;

namespace meteor.UI.Services;

public class EditorLayoutManager : IEditorLayoutManager
{
    private readonly IEditorConfig _config;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IScrollManager _scrollManager;
    private bool _isUpdatingFromScrollManager;

    public EditorLayoutManager(IEditorConfig config, ITextMeasurer textMeasurer, IScrollManager scrollManager)
    {
        _config = config;
        _textMeasurer = textMeasurer;
        _scrollManager = scrollManager;
    }

    public void InitializeLayout(ScrollViewer? scrollViewer, EditorContentControl? contentControl,
        GutterControl? gutterControl)
    {
        _scrollManager.GutterWidth = gutterControl.DesiredSize.Width;
        UpdateViewportAndExtentSizes(scrollViewer, contentControl, gutterControl);
    }

    public void HandleScrollChanged(ScrollViewer? scrollViewer, EditorContentControl? contentControl,
        GutterControl? gutterControl)
    {
        if (_isUpdatingFromScrollManager) return;

        _scrollManager.ScrollOffset = new Vector(scrollViewer.Offset.X, scrollViewer.Offset.Y);
        contentControl.Offset = scrollViewer.Offset;
        gutterControl.UpdateScroll(new Avalonia.Vector(0, scrollViewer.Offset.Y));
    }

    public void HandleSizeChanged(ScrollViewer? scrollViewer, EditorContentControl? contentControl,
        GutterControl? gutterControl)
    {
        UpdateViewportAndExtentSizes(scrollViewer, contentControl, gutterControl);
        contentControl.InvalidateVisual();
    }

    public void HandleScrollManagerChanged(ScrollViewer? scrollViewer, EditorContentControl? contentControl,
        Vector offset)
    {
        _isUpdatingFromScrollManager = true;
        scrollViewer.Offset = new Avalonia.Vector(offset.X, offset.Y);
        _isUpdatingFromScrollManager = false;
        contentControl.InvalidateVisual();
    }

    private void UpdateViewportAndExtentSizes(ScrollViewer? scrollViewer, EditorContentControl? contentControl,
        GutterControl? gutterControl)
    {
        var newViewportSize = new Size(scrollViewer.Viewport.Width, scrollViewer.Viewport.Height);
        var newExtentSize = new Size(scrollViewer.Extent.Width, scrollViewer.Extent.Height);

        _scrollManager.UpdateViewportAndExtentSizes(newViewportSize, newExtentSize);
        contentControl.Viewport = new Avalonia.Size(newViewportSize.Width, newViewportSize.Height);
        gutterControl.Viewport = new Avalonia.Size(gutterControl.Bounds.Width, newViewportSize.Height);
    }
}