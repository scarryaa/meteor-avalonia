using Avalonia.Media;
using meteor.Core.Config;

namespace meteor.UI.Config;

public class AvaloniaEditorConfig : EditorConfig
{
    public Typeface Typeface => new(FontFamily);
    public IBrush TextBrush => new SolidColorBrush(Color.FromArgb(TextColor.A, TextColor.R, TextColor.G, TextColor.B));

    public IBrush BackgroundBrush =>
        new SolidColorBrush(Color.FromArgb(BackgroundColor.A, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B));

    public IBrush CurrentLineHighlightBrush => new SolidColorBrush(Color.FromArgb(CurrentLineHighlightColor.A,
        CurrentLineHighlightColor.R, CurrentLineHighlightColor.G, CurrentLineHighlightColor.B));

    public IBrush SelectionBrush =>
        new SolidColorBrush(Color.FromArgb(SelectionColor.A, SelectionColor.R, SelectionColor.G, SelectionColor.B));
}