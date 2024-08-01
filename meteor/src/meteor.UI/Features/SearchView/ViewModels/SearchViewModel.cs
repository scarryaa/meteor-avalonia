using System.Collections.ObjectModel;
using System.Threading.Tasks;
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
    }
}