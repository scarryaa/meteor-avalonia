using meteor.Core.Interfaces;

namespace meteor.Core.Models.Rendering;

public class Size : ISize
{
    public double Width { get; set; }
    public double Height { get; set; }

    public Size(double width, double height)
    {
        (Width, Height) = (width, height);
    }
}