using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services;

public class GitService : IGitService
{
    private readonly string _repositoryPath;

    public GitService(string repositoryPath)
    {
        _repositoryPath = repositoryPath;
    }

    public async Task<IEnumerable<FileChange>> GetChanges()
    {
        return await Task.Run(() =>
        {
            var changes = new List<FileChange>();
            var gitDir = Path.Combine(_repositoryPath, ".git");

            if (!Directory.Exists(gitDir)) throw new DirectoryNotFoundException("Not a valid Git repository.");

            var indexFile = Path.Combine(gitDir, "index");
            var headFile = Path.Combine(gitDir, "HEAD");

            if (!File.Exists(indexFile) || !File.Exists(headFile))
                throw new FileNotFoundException("Git index or HEAD file not found.");

            var indexLastModified = File.GetLastWriteTime(indexFile);
            var headLastModified = File.GetLastWriteTime(headFile);

            foreach (var file in Directory.EnumerateFiles(_repositoryPath, "*", SearchOption.AllDirectories))
            {
                if (file.StartsWith(Path.Combine(_repositoryPath, ".git"))) continue;

                var fileInfo = new FileInfo(file);
                var relativePath = file.Substring(_repositoryPath.Length).TrimStart(Path.DirectorySeparatorChar);

                if (fileInfo.LastWriteTime > indexLastModified)
                    changes.Add(new FileChange(relativePath, FileChangeType.Modified));
                else if (fileInfo.CreationTime > headLastModified)
                    changes.Add(new FileChange(relativePath, FileChangeType.Added));
            }

            return changes;
        });
    }
}