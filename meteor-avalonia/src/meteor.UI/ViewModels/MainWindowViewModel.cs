using System.ComponentModel;
using System.Runtime.CompilerServices;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.UI.ViewModels;

public sealed class MainWindowViewModel : IMainWindowViewModel
{
    private ITabViewModel _tabViewModel;

    public MainWindowViewModel(ITabViewModel tabViewModel)
    {
        TabViewModel = tabViewModel;
    }

    public ITabViewModel TabViewModel
    {
        get => _tabViewModel;
        set
        {
            if (_tabViewModel != value)
            {
                _tabViewModel = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}