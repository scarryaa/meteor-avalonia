using meteor.Core.Models.EventArgs;

namespace meteor.Core.Interfaces.ViewModels;

public interface IEditorViewModel
{
    int CursorPosition { get; }
    int SelectionStart { get; }
    int SelectionEnd { get; }

    void HandleKeyDown(KeyEventArgs e);
    void HandleTextInput(TextInputEventArgs e);
    int GetLineCount();
    double GetMaxLineWidth();
    string GetContentSlice(int startLine, int endLine);
    string GetEntireContent();
    int GetCursorLine();
    int GetCursorColumn();
    event EventHandler ContentChanged;
    event EventHandler SelectionChanged;
}