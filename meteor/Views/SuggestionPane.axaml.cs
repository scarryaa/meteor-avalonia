using Avalonia.Controls;
using Avalonia.Interactivity;
using meteor.ViewModels;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace meteor.Views;

public partial class SuggestionPane : UserControl
{
    public SuggestionPane()
    {
        InitializeComponent();
    }

    private void OnSuggestionItemDoubleTapped(object sender, RoutedEventArgs e)
    {
        if (DataContext is SuggestionPaneViewModel viewModel &&
            viewModel.SelectedSuggestion is CompletionItem selectedItem)
        {
            viewModel.ApplySelectedSuggestion();
            e.Handled = true;
        }
    }
}