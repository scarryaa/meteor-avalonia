using System.ComponentModel;
using meteor.Core.Models.Events;

namespace meteor.Core.Interfaces.ViewModels;

public interface ITextEditorViewModel : INotifyPropertyChanged, IDisposable
{
    double FontSize { get; set; }
    ITextBuffer TextBuffer { get; }
    public string FontFamily { get; set; }
    double WindowWidth { get; set; }
    double WindowHeight { get; set; }
    int CursorPosition { get; set; }
    double ViewportWidth { get; set; }
    double ViewportHeight { get; set; }
    int SelectionStart { get; set; }
    int SelectionEnd { get; set; }
    bool IsSelecting { get; set; }
    double LongestLineLength { get; }
    double RequiredWidth { get; }
    double RequiredHeight { get; }
    double LineHeight { get; set; }

    event EventHandler<TextChangedEventArgs> TextChanged;
    event EventHandler CursorPositionChanged;
    event EventHandler SelectionChanged;
    event EventHandler InvalidateRequired;

    void InvalidateLongestLine();
    void InsertText(int position, string text);
    void DeleteText(int start, int length);
    void HandleBackspace();
    void HandleDelete();
    void InsertNewLine();
    Task CopyText();
    Task PasteText();
    void OnInvalidateRequired();
    void StartSelection();
    void UpdateSelection();
    void ClearSelection();
    string GetSelectedText();
    void UpdateLineStarts();
}