using Avalonia.Media;
using meteor.Core.Config;

namespace meteor.UI.Config;

public class AvaloniaEditorConfig : EditorConfig
{
    public Typeface Typeface =>
        new("avares://meteor.UI/Common/Assets/Fonts/SanFrancisco/SF-Mono-Medium.otf#SF Mono");
    public IBrush TextBrush => new SolidColorBrush(Color.FromArgb(TextColor.A, TextColor.R, TextColor.G, TextColor.B));

    public IBrush BackgroundBrush =>
        new SolidColorBrush(Color.FromArgb(BackgroundColor.A, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B));

    public IBrush CurrentLineHighlightBrush => new SolidColorBrush(Color.FromArgb(CurrentLineHighlightColor.A,
        CurrentLineHighlightColor.R, CurrentLineHighlightColor.G, CurrentLineHighlightColor.B));

    public IBrush SelectionBrush =>
        new SolidColorBrush(Color.FromArgb(SelectionColor.A, SelectionColor.R, SelectionColor.G, SelectionColor.B));

    public IBrush GutterBackgroundBrush =>
        new SolidColorBrush(Color.FromArgb(GutterBackgroundColor.A, GutterBackgroundColor.R, GutterBackgroundColor.G,
            GutterBackgroundColor.B));

    public IBrush GutterTextBrush =>
        new SolidColorBrush(Color.FromArgb(GutterTextColor.A, GutterTextColor.R, GutterTextColor.G, GutterTextColor.B));
}