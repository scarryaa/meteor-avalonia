using System.Collections.ObjectModel;
using System.ComponentModel;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using meteor.Core.Models.SyntaxHighlighting;

namespace meteor.Core.Interfaces.ViewModels;

public interface IEditorViewModel : INotifyPropertyChanged
{
    string Text { get; set; }
    (int start, int length) Selection { get; }
    int CursorPosition { get; }
    double EditorWidth { get; }
    double EditorHeight { get; }
    Vector ScrollOffset { get; set; }
    ITextBufferService TextBufferService { get; }
    ObservableCollection<SyntaxHighlightingResult> HighlightingResults { get; }
    ITabService TabService { get; }

    void InsertText(int index, string text);
    void DeleteText(int index, int length);

    void UpdateScrollOffset(Vector offset);
    void UpdateWindowSize(double width, double height);
    void OnPointerPressed(PointerPressedEventArgs e);
    void OnPointerMoved(PointerEventArgs e);
    void OnPointerReleased(PointerReleasedEventArgs e);
    void OnTextInput(TextInputEventArgs e);
    Task OnKeyDown(KeyEventArgs e);
}