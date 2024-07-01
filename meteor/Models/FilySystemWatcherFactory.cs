using System;
using System.IO;
using meteor.Interfaces;

namespace meteor.Models;

public class FileSystemWatcherFactory : IFileSystemWatcherFactory
{
    public FileSystemWatcher Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"The specified path does not exist: {path}");

        return new FileSystemWatcher(path);
    }
}