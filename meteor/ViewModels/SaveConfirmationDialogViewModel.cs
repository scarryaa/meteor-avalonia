using System;
using System.Reactive;
using ReactiveUI;

namespace meteor.ViewModels;

public class SaveConfirmationDialogViewModel : ReactiveObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public SaveConfirmationDialogViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;

        SaveCommand = ReactiveCommand.Create(() => CloseDialog(true));
        DiscardCommand = ReactiveCommand.Create(() => CloseDialog(false));
        CancelCommand = ReactiveCommand.Create(() => CloseDialog(null));
    }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> DiscardCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    private void CloseDialog(bool? result)
    {
        _mainWindowViewModel.HideSaveDialogCommand.Execute().Subscribe();
        CloseRequested?.Invoke(this, result);
    }

    public event EventHandler<bool?> CloseRequested;
}