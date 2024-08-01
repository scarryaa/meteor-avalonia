using meteor.Core.Models;

namespace meteor.Core.Interfaces.Services;

public interface IGitService
{
    Task<IEnumerable<FileChange>> GetChanges();
}