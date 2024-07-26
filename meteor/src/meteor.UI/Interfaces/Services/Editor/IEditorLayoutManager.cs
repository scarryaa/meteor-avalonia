using Avalonia.Controls;
using meteor.Core.Models;
using meteor.UI.Controls;

namespace meteor.UI.Interfaces.Services.Editor;

public interface IEditorLayoutManager
{
    void InitializeLayout(ScrollViewer scrollViewer, EditorContentControl contentControl, GutterControl gutterControl);

    void HandleScrollChanged(ScrollViewer scrollViewer, EditorContentControl contentControl,
        GutterControl gutterControl);

    void HandleSizeChanged(ScrollViewer scrollViewer, EditorContentControl contentControl, GutterControl gutterControl);
    void HandleScrollManagerChanged(ScrollViewer scrollViewer, EditorContentControl contentControl, Vector offset);
}