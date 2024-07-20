using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface IGutterViewModel : INotifyPropertyChanged
{
    int VisibleLineCount { get; set; }
    double TotalHeight { get; }
    double ViewportHeight { get; set; }
    int LineCount { get; set; }
    double LineHeight { get; }
    double ScrollOffset { get; set; }
    int CurrentLine { get; set; }
    double GutterWidth { get; }

    event EventHandler<double> ScrollOffsetChanged;

    void ToggleLineCollapse(int lineNumber);
    void ToggleBreakpoint(int lineNumber);
    bool CanCollapseLine(int lineNumber);
    bool IsLineCollapsed(int lineNumber);
    bool HasBreakpoint(int lineNumber);
    void UpdateScrollOffset(double newOffset);
    void UpdateGutterWidth();
}