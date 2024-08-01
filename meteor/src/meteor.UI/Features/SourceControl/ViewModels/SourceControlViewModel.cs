using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.UI.Features.SourceControl.ViewModels;

public partial class SourceControlViewModel : ObservableObject
{
    private readonly IGitService _gitService;

    [ObservableProperty] private ObservableCollection<FileChange> _changes;

    public SourceControlViewModel(IGitService gitService)
    {
        _gitService = gitService;
        Changes = new ObservableCollection<FileChange>();
        LoadDummyChanges();
    }

    [RelayCommand]
    private async Task LoadChanges()
    {
        var changes = await _gitService.GetChanges();
        Changes = new ObservableCollection<FileChange>(changes);
    }

    private void LoadDummyChanges()
    {
        Changes.Add(new FileChange("src/file1.cs", FileChangeType.Modified));
        Changes.Add(new FileChange("src/file2.cs", FileChangeType.Added));
        Changes.Add(new FileChange("src/file3.cs", FileChangeType.Deleted));
        Changes.Add(new FileChange("src/file4.cs", FileChangeType.Renamed));
        Changes.Add(new FileChange("src/file5.cs", FileChangeType.Modified));
    }
}