using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.UI.Features.SourceControl.ViewModels;

public partial class SourceControlViewModel : ObservableObject
{
    private readonly IGitService _gitService;

    [ObservableProperty] private ObservableCollection<FileChange> _changes = new();
    [ObservableProperty] private FileChange _selectedChange;
    [ObservableProperty] private FileChange _hoveredItem;

    public SourceControlViewModel(IGitService gitService) => _gitService = gitService;

    public FileChange SelectedFile
    {
        get => _selectedChange;
        set => SetProperty(ref _selectedChange, value);
    }

    internal void MoveSelection(int direction)
    {
        if (Changes.Count == 0) return;

        int newIndex = (Changes.IndexOf(SelectedFile) + direction + Changes.Count) % Changes.Count;
        SelectedFile = Changes[newIndex];
    }

    [RelayCommand]
    private async Task LoadChangesAsync()
    {
        var changes = _gitService.GetChanges();
        Changes = new ObservableCollection<FileChange>(changes.ToList().DistinctBy(c => c.FilePath));
        SelectedFile = Changes.FirstOrDefault();
    }
}