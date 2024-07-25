using System.Drawing;

namespace meteor.Core.Interfaces.Config;

public interface IEditorConfig
{
    string FontFamily { get; }
    double FontSize { get; }
    double LineHeightMultiplier { get; }
    Color TextColor { get; }
    Color BackgroundColor { get; }
    Color CurrentLineHighlightColor { get; }
    Color SelectionColor { get; }
}