namespace meteor.Core.Interfaces.Services;

public interface IFileService
{
    Task SaveFileAsync(string filePath, string content);
    Task<string> OpenFileAsync(string filePath);
    Task<IEnumerable<object>> SearchInFilesAsync(string searchQuery);
}