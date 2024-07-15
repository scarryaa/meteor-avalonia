namespace meteor.Core.Interfaces;

public interface IPropertyChangedNotifier
{
    void RaisePropertyChanged(object sender, string propertyName);
}