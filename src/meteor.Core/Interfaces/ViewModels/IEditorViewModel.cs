using System.Collections.ObjectModel;
using System.ComponentModel;
using meteor.Core.Models.Events;
using meteor.Core.Models.SyntaxHighlighting;

namespace meteor.Core.Interfaces.ViewModels;

public interface IEditorViewModel : INotifyPropertyChanged
{
    string Text { get; set; }
    (int start, int length) Selection { get; }
    int CursorPosition { get; }
    ObservableCollection<SyntaxHighlightingResult> HighlightingResults { get; }

    void InsertText(int index, string text);
    void DeleteText(int index, int length);

    (double width, double height) CalculateEditorSize(double availableWidth, double availableHeight);
    void UpdateWindowSize(double width, double height);
    void OnPointerPressed(PointerPressedEventArgs e);
    void OnPointerMoved(PointerEventArgs e);
    void OnPointerReleased(PointerReleasedEventArgs e);
    void OnTextInput(TextInputEventArgs e);
    void OnKeyDown(KeyEventArgs e);
}