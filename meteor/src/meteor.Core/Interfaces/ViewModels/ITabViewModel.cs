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
    double ScrollPositionX { get; set; }
    double ScrollPositionY { get; set; }

    void SaveScrollPosition(double positionX, double positionY);
    bool IsTemporary { get; }
    public void SetOriginalContent(string content);
    string FileName { get; }
    void LoadContent(string content);
}