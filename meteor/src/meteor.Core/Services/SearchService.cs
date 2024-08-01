using System.Text.RegularExpressions;
using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.Core.Services
{
    public class SearchService : ISearchService
    {
        private readonly string _projectRoot;
        private readonly string[] _excludedDirectories = { "bin", "obj", ".git", ".vs" };
        private readonly string[] _includedExtensions = { ".cs", ".xaml", ".axaml", ".json", ".xml" };

        public SearchService()
        {
            _projectRoot = Directory.GetCurrentDirectory();
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();

            await Task.Run(() =>
            {
                var files = GetRelevantFiles(_projectRoot);

                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var matches = Regex.Matches(content, query, RegexOptions.IgnoreCase);

                        if (matches.Count > 0)
                        {
                            lock (results)
                            {
                                foreach (Match match in matches)
                                {
                                    int contextStart = Math.Max(0, match.Index - 100);
                                    int contextLength = Math.Min(300, content.Length - contextStart);
                                    string matchContext = content.Substring(contextStart, contextLength);

                                    int lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;

                                    results.Add(new SearchResult
                                    {
                                        FileName = Path.GetFileName(file),
                                        FilePath = file,
                                        LineNumber = lineNumber,
                                        MatchedText = match.Value,
                                        SurroundingContext = matchContext,
                                        LastModified = File.GetLastWriteTime(file),
                                        Relevance = CalculateRelevance(matches.Count, content.Length)
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading file: {file}. Error: {ex.Message}");
                    }
                });
            });

            return results.OrderByDescending(r => r.Relevance);
        }

        private IEnumerable<string> GetRelevantFiles(string root)
        {
            return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(file => _includedExtensions.Contains(Path.GetExtension(file).ToLower()) &&
                               !_excludedDirectories.Any(dir => file.Contains(Path.DirectorySeparatorChar + dir + Path.DirectorySeparatorChar)));
        }

        private double CalculateRelevance(int matchCount, int contentLength)
        {
            // Base relevance on match count and content length
            double baseRelevance = (double)matchCount / contentLength;

            // Apply logarithmic scaling to prevent extreme values
            double scaledRelevance = Math.Log10(baseRelevance * 1000 + 1);

            // Normalize to a 0-1 range
            double normalizedRelevance = scaledRelevance / Math.Log10(1001);

            // Boost relevance for files with higher match density
            double matchDensity = (double)matchCount / (contentLength / 1000);
            double boostedRelevance = normalizedRelevance * (1 + matchDensity);

            return Math.Min(boostedRelevance, 1.0);
        }
    }
}
