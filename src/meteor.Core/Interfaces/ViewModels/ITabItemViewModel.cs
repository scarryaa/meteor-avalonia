using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface ITabItemViewModel : INotifyPropertyChanged, IDisposable
{
    int Index { get; set; }
    IEditorViewModel EditorViewModel { get; }
    string Title { get; set; }
    bool IsDirty { get; set; }
    bool IsSelected { get; set; }
    bool IsTemporary { get; set; }
}