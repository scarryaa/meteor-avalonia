using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Features.SearchView.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly IFileService _fileService;

    [ObservableProperty] private string _searchQuery;

    [ObservableProperty] private ObservableCollection<object> _searchResults;

    public SearchViewModel(IFileService fileService)
    {
        _fileService = fileService;
        SearchResults = new ObservableCollection<object>();
    }

    [RelayCommand]
    private async Task ExecuteSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        SearchResults.Clear();
        var results = await _fileService.SearchInFilesAsync(SearchQuery);
        foreach (var result in results) SearchResults.Add(result);
    }
}