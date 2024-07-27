using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface ITabViewModel : INotifyPropertyChanged
{
    IEditorViewModel EditorViewModel { get; }
    string Title { get; set; }
    bool IsModified { get; set; }
    bool IsActive { get; set; }
}