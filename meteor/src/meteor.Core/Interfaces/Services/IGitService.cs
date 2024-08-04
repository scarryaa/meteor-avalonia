using System;
using System.Collections.Generic;
using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services;

public interface IGitService
{
    event EventHandler<string> RepositoryPathChanged;
    IEnumerable<FileChange> GetChanges();
    string GetRepositoryPath();
    void UpdateProjectRoot(string directoryPath);
    bool IsValidGitRepository(string path);
    bool IsIgnored(string filePath);
    FileChangeType GetFileStatus(string filePath);
}