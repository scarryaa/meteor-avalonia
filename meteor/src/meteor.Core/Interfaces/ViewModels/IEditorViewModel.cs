using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using meteor.Core.Models.EventArgs;
using meteor.Core.Services;

namespace meteor.Core.Interfaces.ViewModels;

public interface IEditorViewModel
{
    ITextBufferService TextBufferService { get; }
    int SelectionStart { get; }
    int SelectionEnd { get; }
    string Content { get; set; }
    List<CompletionItem> CompletionItems { get; }
    bool IsCompletionActive { get; }
    int SelectedCompletionIndex { get; set; }
    int CursorPosition { get; }
    event EventHandler<ContentChangeEventArgs>? ContentChanged;
    event EventHandler? SelectionChanged;
    event EventHandler<int>? CompletionIndexChanged;

    void HandleMouseSelection(int position, bool isShiftPressed);
    Point GetCursorPosition();
    Task TriggerCompletionAsync();
    void ApplySelectedCompletion();
    void MoveCompletionSelection(int delta);
    void CloseCompletion();
    int GetLineCount();
    int GetDocumentLength();
    int GetDocumentVersion();
    double GetMaxLineWidth();
    string GetContentSlice(int start, int end);
    void LoadContent(string content);
    int GetCursorLine();
    int GetCursorColumn();
    double GetCursorX();
    void HandleKeyDown(KeyEventArgs e);
    void HandleTextInput(TextInputEventArgs e);
    void StartSelection(int position);
    void UpdateSelection(int position);
    void EndSelection();
    bool HasSelection();
    void SetCursorPosition(int position);
    int GetLineStartOffset(int lineIndex);
    int GetLineEndOffset(int lineIndex);
    void GoToLineColumn(int line, int column);
}