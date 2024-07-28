using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface ITabViewModel : INotifyPropertyChanged
{
    IEditorViewModel EditorViewModel { get; }
    string Title { get; set; }
    string FilePath { get; }
    string Content { get; set; }
    bool IsModified { get; set; }
    bool IsActive { get; set; }
    bool IsTemporary { get; }
    public void SetOriginalContent(string content);
    string FileName { get; }
    void LoadContent(string content);
}