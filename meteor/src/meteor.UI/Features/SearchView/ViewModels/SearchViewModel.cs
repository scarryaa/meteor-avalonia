using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.UI.Features.SearchView.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;

    [ObservableProperty] private string _searchQuery = string.Empty;

    [ObservableProperty] private ObservableCollection<SearchResult> _searchResults;

    [ObservableProperty] private SearchResult _selectedResult;

    public SearchViewModel(ISearchService searchService)
    {
        _searchService = searchService;
        SearchResults = new ObservableCollection<SearchResult>();
    }

    [RelayCommand]
    private async Task ExecuteSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        SearchResults.Clear();
        var results = await _searchService.SearchAsync(SearchQuery);
        foreach (var result in results)
        {
            SearchResults.Add(result);
        }

        // Reset selected result when performing a new search
        SelectedResult = null;
    }
}