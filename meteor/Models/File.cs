using System.Collections.ObjectModel;
using ReactiveUI;

namespace meteor.Models;

public class File : ReactiveObject
{
    private bool _isExpanded;

    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsDirectory { get; set; }
    public ObservableCollection<File> Items { get; } = new();

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }
}