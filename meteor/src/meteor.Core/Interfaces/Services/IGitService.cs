using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services;

public interface IGitService
{
    event EventHandler<string> RepositoryPathChanged;
    IEnumerable<FileChange> GetChanges();
    string GetRepositoryPath();
    void UpdateProjectRoot(string directoryPath);
}