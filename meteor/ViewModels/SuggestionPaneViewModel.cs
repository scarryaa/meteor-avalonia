using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using ReactiveUI;

namespace meteor.ViewModels;

public class SuggestionPaneViewModel : ViewModelBase
{
    private ObservableCollection<CompletionItem> _suggestions;
    private CompletionItem _selectedSuggestion;

    public event EventHandler<CompletionItem> SuggestionApplied;

    public ObservableCollection<CompletionItem> Suggestions
    {
        get => _suggestions;
        set => this.RaiseAndSetIfChanged(ref _suggestions, value);
    }

    public CompletionItem SelectedSuggestion
    {
        get => _selectedSuggestion;
        set => this.RaiseAndSetIfChanged(ref _selectedSuggestion, value);
    }

    public ReactiveCommand<Unit, Unit> ApplySuggestionCommand { get; }

    public SuggestionPaneViewModel()
    {
        Suggestions = new ObservableCollection<CompletionItem>();
        ApplySuggestionCommand = ReactiveCommand.Create(ApplySelectedSuggestion);
    }

    public void UpdateSuggestions(IEnumerable<CompletionItem> items)
    {
        Suggestions.Clear();
        foreach (var item in items) Suggestions.Add(item);
    }

    public void ApplySelectedSuggestion()
    {
        if (SelectedSuggestion != null) SuggestionApplied?.Invoke(this, SelectedSuggestion);
    }
}