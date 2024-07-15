using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Interfaces.Rendering;

public interface IRenderManager : IDisposable
{
    void UpdateFilePath(string filePath);
    void AttachToViewModel(IScrollableTextEditorViewModel viewModel);
    Task InitializeAsync(string initialText);
    void MarkLineDirty(int lineIndex);
    void Render(IDrawingContext context);
    void InvalidateLine(int lineIndex);
    void InvalidateLines(int startLine, int endLine);
    ValueTask UpdateSyntaxHighlightingAsync(string text, int startLine = 0, int endLine = -1);
}