namespace meteor.Core.Models;

public class FileChange
{
    public FileChange(string filePath, FileChangeType changeType)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        ChangeType = changeType;
    }

    public string FilePath { get; set; }
    public FileChangeType ChangeType { get; set; }
}