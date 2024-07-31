using Avalonia.Controls;
using meteor.Core.Models;
using meteor.UI.Features.Editor.Controls;
using meteor.UI.Features.Gutter.Controls;

namespace meteor.UI.Features.Editor.Interfaces;

public interface IEditorLayoutManager
{
    void InitializeLayout(ScrollViewer? scrollViewer, EditorContentControl? contentControl,
        GutterControl? gutterControl);

    void HandleScrollChanged(ScrollViewer? scrollViewer, EditorContentControl? contentControl,
        GutterControl? gutterControl);

    void HandleSizeChanged(ScrollViewer? scrollViewer, EditorContentControl? contentControl,
        GutterControl? gutterControl);

    void HandleScrollManagerChanged(ScrollViewer? scrollViewer, EditorContentControl? contentControl, Vector offset);
}