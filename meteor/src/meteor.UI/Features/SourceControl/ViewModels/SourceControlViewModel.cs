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
    }

    [RelayCommand]
    private async Task LoadChanges()
    {
        var changes = await _gitService.GetChanges();
        Changes = new ObservableCollection<FileChange>(changes);
    }
}