using System.IO;

namespace meteor.Interfaces;

public interface IFileSystemWatcherFactory
{
    FileSystemWatcher Create(string path);
}