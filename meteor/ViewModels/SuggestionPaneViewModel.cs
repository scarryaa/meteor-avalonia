using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using ReactiveUI;

namespace meteor.ViewModels;

public class SuggestionPaneViewModel : ViewModelBase
{
    private ObservableCollection<CompletionItem> _suggestions;

    public ObservableCollection<CompletionItem> Suggestions
    {
        get => _suggestions;
        set => this.RaiseAndSetIfChanged(ref _suggestions, value);
    }

    private CompletionItem _selectedSuggestion;

    public CompletionItem SelectedSuggestion
    {
        get => _selectedSuggestion;
        set => this.RaiseAndSetIfChanged(ref _selectedSuggestion, value);
    }

    public SuggestionPaneViewModel()
    {
        Suggestions = new ObservableCollection<CompletionItem>();
    }

    public void UpdateSuggestions(IEnumerable<CompletionItem> items)
    {
        Suggestions.Clear();
        foreach (var item in items) Suggestions.Add(item);
    }
}