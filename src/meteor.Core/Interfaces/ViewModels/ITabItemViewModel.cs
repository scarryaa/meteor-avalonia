using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface ITabItemViewModel : INotifyPropertyChanged, IDisposable
{
    IEditorViewModel EditorViewModel { get; }
    string Title { get; set; }
    bool IsDirty { get; set; }
    bool IsSelected { get; set; }
    bool IsTemporary { get; set; }
}