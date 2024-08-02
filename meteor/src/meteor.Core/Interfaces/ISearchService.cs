using meteor.Core.Models;

namespace meteor.Core.Interfaces
{
    public interface ISearchService
    {
        Task<IEnumerable<SearchResult>> SearchAsync(string query);
        void UpdateFilter(string filterName, bool isActive);
        void UpdateProjectRoot(string directoryPath);
    }
}
