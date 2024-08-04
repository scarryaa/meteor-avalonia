using System.Text.RegularExpressions;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using System.Collections.Concurrent;

namespace meteor.Core.Services
{
    public class GitService : IGitService
    {
        private string _repoPath;
        private HashSet<string> _ignoredPatterns;
        private bool _isValidRepo;
        private ConcurrentDictionary<string, FileChangeType> _fileStatusCache = new ConcurrentDictionary<string, FileChangeType>();

        public event EventHandler<string> RepositoryPathChanged;

        public GitService(string repoPath)
        {
            UpdateProjectRoot(repoPath);
            _fileStatusCache = new ConcurrentDictionary<string, FileChangeType>();
        }

        public IEnumerable<FileChange> GetChanges()
        {
            if (!_isValidRepo)
            {
                return Enumerable.Empty<FileChange>();
            }

            var changes = new List<FileChange>();
            var gitStatusOutput = ExecuteGitCommand("status --porcelain");

            foreach (var line in gitStatusOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length < 4) continue;

                var status = line.Substring(0, 2).Trim();
                var filePath = line.Substring(3).Trim();

                if (IsIgnored(filePath)) continue;

                var changeType = GetChangeType(status);
                changes.Add(new FileChange(filePath, changeType));
                _fileStatusCache[filePath] = changeType;
            }

            return changes;
        }

        public void UpdateProjectRoot(string directoryPath)
        {
            if (_repoPath != directoryPath)
            {
                _repoPath = directoryPath;
                _isValidRepo = IsValidGitRepository(_repoPath);
                _ignoredPatterns = _isValidRepo ? LoadGitIgnore() : new HashSet<string>();
                _fileStatusCache.Clear();
                RepositoryPathChanged?.Invoke(this, _repoPath);
            }
        }

        private bool IsValidGitRepository(string path)
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            var gitDir = Path.Combine(path, ".git");
            return Directory.Exists(gitDir);
        }

        private FileChangeType GetChangeType(string status)
        {
            return status switch
            {
                "A" or "??" => FileChangeType.Added,
                "M" => FileChangeType.Modified,
                "D" => FileChangeType.Deleted,
                "R" => FileChangeType.Renamed,
                _ => FileChangeType.Untracked,
            };
        }

        public bool IsIgnored(string path)
        {
            if (!_isValidRepo)
            {
                return false;
            }

            string relativePath = Path.GetRelativePath(_repoPath, path);
            string normalizedPath = relativePath.Replace('\\', '/');

            foreach (var pattern in _ignoredPatterns)
            {
                if (IsMatch(normalizedPath, pattern))
                {
                    return true;
                }
            }

            // Check if any parent directory is ignored
            string[] pathParts = normalizedPath.Split('/');
            for (int i = 1; i < pathParts.Length; i++)
            {
                string parentPath = string.Join('/', pathParts.Take(i));
                if (_ignoredPatterns.Contains(parentPath + '/'))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsMatch(string filePath, string pattern)
        {
            pattern = pattern.Replace(".", "\\.").Replace("*", ".*").Replace("?", ".");
            return Regex.IsMatch(filePath, "^" + pattern + "$", RegexOptions.IgnoreCase);
        }

        private HashSet<string> LoadGitIgnore()
        {
            var ignoredPatterns = new HashSet<string>();
            var gitIgnorePath = Path.Combine(_repoPath, ".gitignore");

            if (File.Exists(gitIgnorePath))
            {
                var lines = File.ReadAllLines(gitIgnorePath);
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("#"))
                    {
                        ignoredPatterns.Add(trimmedLine);
                    }
                }
            }

            return ignoredPatterns;
        }

        private string ExecuteGitCommand(string arguments)
        {
            if (!_isValidRepo)
            {
                return string.Empty;
            }

            var gitDir = Path.Combine(_repoPath, ".git");
            if (!Directory.Exists(gitDir))
            {
                _isValidRepo = false;
                return string.Empty;
            }

            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _repoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = System.Diagnostics.Process.Start(processStartInfo);
                return process?.StandardOutput.ReadToEnd() ?? string.Empty;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Git is not installed or not in the PATH
                _isValidRepo = false;
                return string.Empty;
            }
        }

        public string GetRepositoryPath()
        {
            return _repoPath;
        }

        bool IGitService.IsValidGitRepository(string path)
        {
            return IsValidGitRepository(path);
        }

        public FileChangeType GetFileStatus(string dir)
        {
            if (!_isValidRepo || string.IsNullOrEmpty(dir))
            {
                return FileChangeType.Untracked;
            }

            var relativePath = Path.GetRelativePath(_repoPath, dir);

            // Check if the status is already cached
            if (_fileStatusCache.TryGetValue(relativePath, out var cachedStatus))
            {
                return cachedStatus;
            }

            var gitStatusOutput = ExecuteGitCommand($"status --porcelain \"{relativePath}\"");

            var changeType = FileChangeType.Untracked;

            if (!string.IsNullOrWhiteSpace(gitStatusOutput))
            {
                var statusCode = gitStatusOutput.Substring(0, 2).Trim();
                changeType = GetChangeType(statusCode);
            }

            // Cache the result
            _fileStatusCache[relativePath] = changeType;

            return changeType;
        }
    }
}