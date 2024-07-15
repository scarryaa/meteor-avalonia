using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface ILineCountViewModel : INotifyPropertyChanged
{
    int LineCount { get; set; }
    double VerticalOffset { get; set; }
    double ViewportHeight { get; set; }
    int MaxLineNumber { get; }
    void UpdateLineCount(int newLineCount);
}