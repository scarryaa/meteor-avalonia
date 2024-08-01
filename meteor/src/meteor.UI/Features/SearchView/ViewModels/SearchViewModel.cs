using System.Collections.ObjectModel;
using System.Collections.Generic;
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
    [ObservableProperty] private Dictionary<string, List<SearchResult>> _groupedItems;
    [ObservableProperty] private HashSet<string> _collapsedGroups = new HashSet<string>();
    [ObservableProperty] private double _totalContentHeight;
    [ObservableProperty] private SearchResult _hoveredItem;
    [ObservableProperty] private string _hoveredHeader;

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

        UpdateGroupedItems();
        SelectedResult = null;
    }

    private void UpdateGroupedItems()
    {
        GroupedItems = SearchResults
            .GroupBy(r => r.FileName)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public void ToggleGroupCollapse(string groupKey)
    {
        if (CollapsedGroups.Contains(groupKey))
        {
            CollapsedGroups.Remove(groupKey);
        }
        else
        {
            CollapsedGroups.Add(groupKey);
        }
    }

    public void MoveSelection(int direction)
    {
        var flatResults = SearchResults.ToList();
        var currentIndex = flatResults.IndexOf(SelectedResult);
        var newIndex = (currentIndex + direction + flatResults.Count) % flatResults.Count;
        SelectedResult = flatResults[newIndex];
    }

    public void UpdateProjectRoot(string projectRoot)
    {
        Console.WriteLine("UpdateProjectRoot called with projectRoot: " + projectRoot);
        _searchService.UpdateProjectRoot(projectRoot);
    }
}