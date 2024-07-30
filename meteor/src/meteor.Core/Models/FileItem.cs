namespace meteor.Core.Models;

public class FileItem
{
    public string Name { get; }
    public string FullPath { get; }
    public List<FileItem> Children { get; } = [];
    public bool IsExpanded { get; set; }
    public bool IsDirectory { get; }
    public bool ChildrenPopulated { get; set; }

    public FileItem(string fullPath, bool isDirectory)
    {
        FullPath = fullPath;
        Name = Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(Name) && isDirectory) Name = fullPath;
        IsDirectory = isDirectory;
    }

    public FileItem(string name, string fullPath, bool isDirectory)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
    }
}