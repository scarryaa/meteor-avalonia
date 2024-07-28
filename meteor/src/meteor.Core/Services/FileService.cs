using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class FileService : IFileService
{
    public async Task SaveFileAsync(string filePath, string content)
    {
        await File.WriteAllTextAsync(filePath, content);
    }

    public async Task<string> OpenFileAsync(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }
}