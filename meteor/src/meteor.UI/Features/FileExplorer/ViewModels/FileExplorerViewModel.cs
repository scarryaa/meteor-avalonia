using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using meteor.Core.Models;
using meteor.Core.Interfaces.Services;
using meteor.UI.Features.FileExplorer.Services;

public partial class FileExplorerViewModel : ObservableObject
{
    private readonly IGitService _gitService;
    private readonly FileSystemHelper _fileSystemHelper;
    private const int MaxItemsPerDirectory = 1000;

    [ObservableProperty]
    private ObservableCollection<FileItem> _items;

    [ObservableProperty]
    private FileItem _selectedItem;

    private ConcurrentDictionary<string, Task> _populationTasks = new ConcurrentDictionary<string, Task>();

    public FileExplorerViewModel(IGitService gitService)
    {
        _gitService = gitService;
        _fileSystemHelper = new FileSystemHelper(_gitService);
        _items = new ObservableCollection<FileItem>();
    }

    public void SetDirectory(string path)
    {
        _items.Clear();
        _items.Add(new FileItem(path, true));
        _ = PopulateChildrenAsync(_items[0]);
        _items[0].IsExpanded = true;
    }

    private async Task PopulateChildrenAsync(FileItem item)
    {
        if (item.ChildrenPopulated) return;

        try
        {
            var task = _populationTasks.GetOrAdd(item.FullPath, _ => Task.Run(() => PopulateChildrenInternal(item)));
            await task;
            _populationTasks.TryRemove(item.FullPath, out _);
        }
        catch (Exception ex)
        {
            item.Children.Add(new FileItem($"Error: {ex.Message}", item.FullPath, false));
        }
    }

    private void PopulateChildrenInternal(FileItem item)
    {
        var children = new List<FileItem>();
        var directories = _fileSystemHelper.GetDirectories(item.FullPath);
        var files = _fileSystemHelper.GetFiles(item.FullPath);

        children.AddRange(directories.Take(MaxItemsPerDirectory / 2));
        children.AddRange(files.Take(MaxItemsPerDirectory / 2));

        if (children.Count > MaxItemsPerDirectory)
        {
            children = children.Take(MaxItemsPerDirectory).ToList();
            children.Add(new FileItem("... (More items not shown)", item.FullPath, false));
        }

        item.Children.Clear();
        foreach (var child in children)
        {
            item.Children.Add(child);
        }
        item.ChildrenPopulated = true;
    }

    public void ToggleDirectoryExpansion(FileItem directory)
    {
        directory.IsExpanded = !directory.IsExpanded;
        if (directory.IsExpanded && !directory.ChildrenPopulated) _ = PopulateChildrenAsync(directory);
    }

    public void RefreshAllFileStatuses()
    {
        UpdateAllItemStatuses(_items);
    }

    private void UpdateAllItemStatuses(IEnumerable<FileItem> items)
    {
        foreach (var item in items)
        {
            var newStatus = _gitService.GetFileStatus(item.FullPath);
            if (item.GitStatus != newStatus)
            {
                item.GitStatus = newStatus;
                if (item.IsDirectory)
                {
                    UpdateDirectoryStatus(item);
                }
            }
            if (item.IsDirectory && item.IsExpanded)
            {
                UpdateAllItemStatuses(item.Children);
            }
        }
    }

    private void UpdateDirectoryStatus(FileItem directory)
    {
        if (!directory.IsDirectory)
            return;

        var childStatuses = directory.Children
            .Select(child => child.GitStatus)
            .Where(status => status != null)
            .Select(status => (FileChangeType)status)
            .ToList();

        if (childStatuses.Count == 0)
        {
            directory.GitStatus = null;
        }
        else if (childStatuses.Contains(FileChangeType.Modified))
        {
            directory.GitStatus = FileChangeType.Modified;
        }
        else if (childStatuses.Contains(FileChangeType.Added))
        {
            directory.GitStatus = FileChangeType.Added;
        }
        else if (childStatuses.Contains(FileChangeType.Deleted))
        {
            directory.GitStatus = FileChangeType.Deleted;
        }
        else if (childStatuses.Contains(FileChangeType.Renamed))
        {
            directory.GitStatus = FileChangeType.Renamed;
        }
        else
        {
            directory.GitStatus = childStatuses[0];
        }
    }
}