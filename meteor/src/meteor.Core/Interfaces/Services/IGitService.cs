using System.Collections.Generic;
using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services;

public interface IGitService
{
    IEnumerable<FileChange> GetChanges();
    void UpdateProjectRoot(string directoryPath);
}