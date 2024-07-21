using System.Collections.ObjectModel;
using System.ComponentModel;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using meteor.Core.Models.SyntaxHighlighting;

namespace meteor.Core.Interfaces.ViewModels;

public interface IEditorViewModel : INotifyPropertyChanged, IDisposable
{
    event EventHandler InvalidateMeasureRequested;

    int CurrentLine { get; set; }
    string Text { get; set; }
    (int start, int length) Selection { get; set; }
    int CursorPosition { get; set; }
    double EditorWidth { get; }
    double EditorHeight { get; }
    Vector ScrollOffset { get; set; }
    double MaxScrollHeight { get; set; }
    ObservableCollection<SyntaxHighlightingResult> HighlightingResults { get; }
    ITabService TabService { get; }
    ITextBufferService TextBufferService { get; }
    
    void DeleteText(int index, int length);


    void DispatcherInvoke(Action action);
    void SuppressNotifications(bool suppress);
    void RaiseInvalidateMeasure();
    void UpdateScrollOffset(Vector offset);
    void UpdateEditorSize(double width, double height, double viewportHeight, double viewportWidth);
    void UpdateWindowSize(double width, double height);
    void OnPointerPressed(PointerPressedEventArgs e);
    void OnPointerMoved(PointerEventArgs e);
    void OnPointerReleased(PointerReleasedEventArgs e);
    void OnTextInput(TextInputEventArgs e);
    Task OnKeyDown(KeyEventArgs e);
}