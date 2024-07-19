using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface IMainWindowViewModel : INotifyPropertyChanged
{
    ITabViewModel TabViewModel { get; set; }
}