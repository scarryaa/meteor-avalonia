using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.Core.Services
{
    public class SearchService : ISearchService
    {
        private readonly string _projectRoot;

        public SearchService()
        {
            _projectRoot = Directory.GetCurrentDirectory();
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();

            await Task.Run(() =>
            {
                var files = Directory.GetFiles(_projectRoot, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        if (content.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(new SearchResult
                            {
                                Title = Path.GetFileName(file),
                                Description = $"File contains the search query: {query}",
                                Url = file,
                                LastModified = File.GetLastWriteTime(file),
                                Relevance = 1.0
                            });
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Error reading file: {file}");
                    }
                }
            });

            return results.OrderByDescending(r => r.Relevance);
        }
    }
}


