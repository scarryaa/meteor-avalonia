using Avalonia.Media.Imaging;

namespace meteor.Views.Models;

public class CachedLinePart
{
    public int StartColumn { get; set; }
    public int EndColumn { get; set; }
    public WriteableBitmap Image { get; set; }
}