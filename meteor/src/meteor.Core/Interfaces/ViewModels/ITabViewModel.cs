using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface ITabViewModel : INotifyPropertyChanged
{
    IEditorViewModel EditorViewModel { get; }
    string Title { get; set; }
    string FilePath { get; set; }
    string Content { get; set; }
    bool IsModified { get; set; }
    bool IsActive { get; set; }
    double ScrollPositionX { get; set; }
    double ScrollPositionY { get; set; }
    bool IsTemporary { get; }
    string FileName { get; set; }

    void SetFilePath(string filePath);
    void SaveScrollPosition(double positionX, double positionY);
    public void SetOriginalContent(string content);
    void LoadContent(string content);
}