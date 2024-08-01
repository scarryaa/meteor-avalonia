using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;

namespace meteor.Core.Services
{
    public class GitService : IGitService
    {
        private string _repoPath;
        private HashSet<string> _ignoredPatterns;

        public GitService(string repoPath)
        {
            _repoPath = repoPath;
            _ignoredPatterns = LoadGitIgnore();
        }

        public IEnumerable<FileChange> GetChanges()
        {
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
            }

            return changes;
        }

        public void UpdateProjectRoot(string directoryPath)
        {
            _repoPath = directoryPath;
            _ignoredPatterns = LoadGitIgnore();
        }

        private FileChangeType GetChangeType(string status)
        {
            return status switch
            {
                "A" => FileChangeType.Added,
                "M" => FileChangeType.Modified,
                "D" => FileChangeType.Deleted,
                "R" => FileChangeType.Renamed,
                _ => FileChangeType.Modified,
            };
        }

        private bool IsIgnored(string filePath)
        {
            return _ignoredPatterns.Any(pattern => IsMatch(filePath, pattern));
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
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _repoPath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            return process?.StandardOutput.ReadToEnd() ?? string.Empty;
        }
    }
}

