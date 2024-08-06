using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.UI.Features.FileExplorer.Services;

public class FileSystemHelper
{
    private readonly IGitService _gitService;
    private readonly ConcurrentDictionary<string, FileChangeType?> _fileStatusCache;

    public FileSystemHelper(IGitService gitService)
    {
        _gitService = gitService;
        _fileStatusCache = new ConcurrentDictionary<string, FileChangeType?>();
    }

    public IEnumerable<FileItem> GetDirectories(string path)
    {
        return Directory.EnumerateDirectories(path)
            .Where(dir => !Path.GetFileName(dir).StartsWith('.'))
            .Select(dir => new FileItem(dir, true) { GitStatus = GetFileStatus(dir) })
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<FileItem> GetFiles(string path)
    {
        return Directory.EnumerateFiles(path)
            .Where(file => !ShouldHideFile(file))
            .Select(file => new FileItem(file, false) { GitStatus = GetFileStatus(file) })
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase);
    }

    private FileChangeType? GetFileStatus(string path)
    {
        return _fileStatusCache.GetOrAdd(path, p => _gitService.GetFileStatus(p));
    }

    public bool ShouldHideFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var directoryName = Path.GetDirectoryName(filePath);

        var hiddenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".DS_Store",
            "Thumbs.db",
            "desktop.ini"
        };

        // Ignore all files and directories within .git
        if (directoryName != null && directoryName.Split(Path.DirectorySeparatorChar).Contains(".git"))
        {
            return true;
        }

        // Ignore .git directory itself
        if (fileName == ".git")
        {
            return true;
        }

        return fileName.StartsWith('.') || hiddenFiles.Contains(fileName);
    }
}