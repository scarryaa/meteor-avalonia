using System.Collections.Concurrent;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace meteor.UI.Features.FileExplorer.Services;

public class GitStatusManager
{
    private readonly IGitService _gitService;
    private readonly ConcurrentDictionary<string, FileChangeType?> _fileStatusCache;

    public GitStatusManager(IGitService gitService)
    {
        _gitService = gitService;
        _fileStatusCache = new ConcurrentDictionary<string, FileChangeType?>();
    }

    public FileChangeType? GetFileStatus(string path)
    {
        return _fileStatusCache.GetOrAdd(path, p => _gitService.GetFileStatus(p));
    }

    public void ClearCache()
    {
        _fileStatusCache.Clear();
    }

    public void UpdateAllItemStatuses(IEnumerable<FileItem> items)
    {
        foreach (var item in items)
        {
            var newStatus = GetFileStatus(item.FullPath);
            if (item.GitStatus != newStatus)
            {
                item.GitStatus = newStatus;
                _fileStatusCache[item.FullPath] = newStatus;
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

    public void UpdateDirectoryStatus(FileItem directory)
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
            directory.GitStatus = childStatuses.First();
        }
    }
}