namespace meteor.Core.Interfaces.Rendering;

public interface IRenderedLine
{
    IImage Image { get; }
    void Invalidate();
}