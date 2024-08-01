using System.Collections.Generic;
using System.Threading.Tasks;
using meteor.Core.Models;

namespace meteor.Core.Interfaces
{
    public interface ISearchService
    {
        Task<IEnumerable<SearchResult>> SearchAsync(string query);
    }
}
