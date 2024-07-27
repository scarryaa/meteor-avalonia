using System.Windows.Input;
using meteor.Core.Interfaces.Models;

namespace meteor.Core.Interfaces.Config;

public interface ITabViewModelConfig
{
    ISolidColorBrush GetBorderBrush();
    ISolidColorBrush GetBackground();
    ISolidColorBrush GetForeground();
    ISolidColorBrush GetDirtyIndicatorBrush();
    ISolidColorBrush GetCloseButtonForeground();
    ISolidColorBrush GetCloseButtonBackground();
    ICommand GetCloseTabCommand();
}