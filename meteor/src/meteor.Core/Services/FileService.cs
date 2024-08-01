using System.Text.RegularExpressions;
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

    public async Task<IEnumerable<object>> SearchInFilesAsync(string searchQuery)
    {
        var results = new List<object>();
        var directory = Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var matches = Regex.Matches(content, searchQuery, RegexOptions.IgnoreCase);

            if (matches.Count > 0)
                results.Add(new
                {
                    FilePath = file,
                    MatchCount = matches.Count,
                    Matches = matches.Select(m => new
                    {
                        LineNumber = content.Substring(0, m.Index).Count(c => c == '\n') + 1,
                        Content = m.Value
                    })
                });
        }

        return results;
    }
}