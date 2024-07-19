namespace meteor.Core.Interfaces.Services;

public interface IEditorSizeCalculator
{
    (double width, double height) CalculateEditorSize(ITextBufferService textBufferService, double windowWidth,
        double windowHeight);

    void UpdateWindowSize(double width, double height);
    void InvalidateCache();
}