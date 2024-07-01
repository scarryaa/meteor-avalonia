using Avalonia.Controls;
using meteor.ViewModels;

namespace meteor.Views;

public partial class SaveConfirmationDialog : Window
{
    public SaveConfirmationDialog(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();
        DataContext = new SaveConfirmationDialogViewModel(mainWindowViewModel);
        ((SaveConfirmationDialogViewModel)DataContext).CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object sender, bool? e)
    {
        Close(e);
    }
}