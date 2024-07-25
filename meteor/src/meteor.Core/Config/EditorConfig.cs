using System.Drawing;
using meteor.Core.Interfaces.Config;

namespace meteor.Core.Config;

public class EditorConfig : IEditorConfig
{
    public string FontFamily { get; set; } = "San Francisco Mono";
    public double FontSize { get; set; } = 13;
    public double LineHeightMultiplier { get; set; } = 1.5;
    public Color TextColor { get; set; } = Color.Black;
    public Color BackgroundColor { get; set; } = Color.White;
    public Color CurrentLineHighlightColor { get; set; } = Color.FromArgb(237, 237, 237); // #ededed
    public Color SelectionColor { get; set; } = Color.FromArgb(100, 139, 205, 205);

    public Color GutterBackgroundColor { get; set; } = Color.FromArgb(255, 255, 255);
    public Color GutterTextColor { get; set; } = Color.FromArgb(100, 100, 100); // #666
}