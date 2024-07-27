using meteor.Core.Interfaces.Services;
using meteor.Core.Models.EventArgs;

namespace meteor.Core.Interfaces.ViewModels;

public interface IEditorViewModel
{
    event EventHandler<ContentChangeEventArgs>? ContentChanged;
    event EventHandler? SelectionChanged;

    int SelectionStart { get; }
    int SelectionEnd { get; }
    ITextBufferService TextBufferService { get; }
    bool HasSelection();
    int CursorPosition { get; }
    string Content { get; set; }

    int GetDocumentLength();
    int GetDocumentVersion();
    int GetLineCount();
    double GetMaxLineWidth();
    string GetContentSlice(int start, int end);
    int GetCursorLine();
    int GetCursorColumn();
    double GetCursorX();

    void HandleKeyDown(KeyEventArgs e);
    void HandleTextInput(TextInputEventArgs e);

    void StartSelection(int position);
    void UpdateSelection(int position);
    void EndSelection();
    void SetCursorPosition(int position);
    int GetLineStartOffset(int lineIndex);
    int GetLineEndOffset(int lineIndex);
}