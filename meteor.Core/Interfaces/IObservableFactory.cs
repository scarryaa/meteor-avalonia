namespace meteor.Core.Interfaces;

public interface IObservableFactory
{
    IObservable<T> FromPropertyChanged<T>(T instance, params string[] propertyNames);
}