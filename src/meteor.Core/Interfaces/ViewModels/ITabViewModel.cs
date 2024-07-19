using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace meteor.Core.Interfaces.ViewModels;

public interface ITabViewModel : INotifyPropertyChanged, IDisposable
{
    public ObservableCollection<ITabItemViewModel> Tabs { get; set; }
    ITabItemViewModel SelectedTab { get; set; }
    ICommand AddTabCommand { get; }
    ICommand CloseTabCommand { get; }
    ICommand CloseAllTabsCommand { get; }
    ICommand CloseOtherTabsCommand { get; }
}