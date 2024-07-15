using System.ComponentModel;

namespace meteor.Core.Interfaces.ViewModels;

public interface IGutterViewModel : INotifyPropertyChanged, IDisposable
{
    IScrollManager ScrollManager { get; set; }
    void OnInvalidateRequired();
}