using meteor.Core.Interfaces.Rendering;

namespace meteor.Core.Interfaces.Commands;

public interface ITextEditorCommands
{
    void InsertText(int position, string text);
    void HandleBackspace();
    void HandleDelete();
    void InsertNewLine();
    Task CopyText();
    Task PasteText();
    Task CutText();
    void Undo();
    void Redo();
    int GetPositionFromPoint(IPoint? point);
}