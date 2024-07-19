using System;
using System.Windows.Input;

namespace meteor.UI.Factories;

public interface ICommandFactory
{
    ICommand CreateCommand(Action execute);
    ICommand CreateCommand<T>(Action<T> execute);
}

public class CommandFactory : ICommandFactory
{
    public ICommand CreateCommand(Action execute)
    {
        return new RelayCommand(execute);
    }

    public ICommand CreateCommand<T>(Action<T> execute)
    {
        return new RelayCommand<T>(execute);
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        return true;
    }

    public void Execute(object parameter)
    {
        _execute();
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;

    public RelayCommand(Action<T> execute)
    {
        _execute = execute;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        return true;
    }

    public void Execute(object parameter)
    {
        _execute((T)parameter);
    }
}