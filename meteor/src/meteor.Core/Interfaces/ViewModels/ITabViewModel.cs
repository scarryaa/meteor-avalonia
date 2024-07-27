using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface ITabViewModel : INotifyPropertyChanged
{
    IEditorViewModel EditorViewModel { get; }
    string Title { get; set; }
    string Content { get; set; }
    bool IsModified { get; set; }
    bool IsActive { get; set; }
    string FileName { get; }
}