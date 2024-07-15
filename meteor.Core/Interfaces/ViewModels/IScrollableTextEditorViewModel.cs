using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface IScrollableTextEditorViewModel : INotifyPropertyChanged
{
    ITextEditorViewModel TextEditorViewModel { get; }
    ILineCountViewModel LineCountViewModel { get; }
    IGutterViewModel GutterViewModel { get; }

    double FontSize { get; set; }
    double LineHeight { get; set; }
    double LongestLineWidth { get; }
    double TotalHeight { get; }
    double WindowHeight { get; set; }
    double WindowWidth { get; set; }
    double ViewportHeight { get; set; }
    double ViewportWidth { get; set; }
    double VerticalOffset { get; set; }
    double HorizontalOffset { get; set; }

    void UpdateViewProperties();
}