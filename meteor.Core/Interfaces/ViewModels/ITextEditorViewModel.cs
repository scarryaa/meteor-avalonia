using System.ComponentModel;
using meteor.Core.Models.Events;

namespace meteor.Core.Interfaces.ViewModels;

public interface ITextEditorViewModel : INotifyPropertyChanged, IDisposable
{
    #region Text Properties
    ITextBuffer TextBuffer { get; }
    double LongestLineLength { get; }

    #endregion

    #region Font Properties

    double FontSize { get; set; }
    string FontFamily { get; set; }
    double LineHeight { get; set; }

    #endregion

    #region Layout Properties
    double WindowWidth { get; set; }
    double WindowHeight { get; set; }
    double ViewportWidth { get; set; }
    double ViewportHeight { get; set; }
    double RequiredWidth { get; }
    double RequiredHeight { get; }

    #endregion

    #region Cursor and Selection Properties

    int CursorPosition { get; set; }
    int SelectionStart { get; set; }
    int SelectionEnd { get; set; }
    bool IsSelecting { get; set; }

    #endregion

    #region Events

    event EventHandler<TextChangedEventArgs>? TextChanged;
    event EventHandler<CursorPositionChangedEventArgs>? CursorPositionChanged;
    event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
    event EventHandler InvalidateRequired;

    #endregion

    #region Text Manipulation Methods
    void InsertText(int position, string text);
    void DeleteText(int start, int length);
    void HandleBackspace();
    void HandleDelete();
    void InsertNewLine();

    #endregion

    #region Clipboard Operations
    Task CopyText();
    void PasteText(string text);

    #endregion

    #region Selection Methods
    void StartSelection();
    void UpdateSelection();
    void ClearSelection();
    string GetSelectedText();

    #endregion

    #region Layout and Rendering Methods

    void InvalidateLongestLine();
    void OnInvalidateRequired();
    void UpdateLineStarts();
    void UpdateLongestLineWidth();

    #endregion
}