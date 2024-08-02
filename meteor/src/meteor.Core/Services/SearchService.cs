using System.Text.RegularExpressions;
using meteor.Core.Interfaces;
using meteor.Core.Models;

namespace meteor.Core.Services
{
    public class SearchService : ISearchService
    {
        private string _projectRoot;
        private Dictionary<string, bool> _filters;
        private readonly string[] _excludedDirectories = { "bin", "obj", ".git", ".vs" };
        private readonly string[] _includedExtensions = { ".cs", ".xaml", ".axaml", ".json", ".xml" };

        public SearchService()
        {
            _projectRoot = Directory.GetCurrentDirectory();
            _filters = new Dictionary<string, bool>
            {
                { "Match Case", false },
                { "Regex", false },
                { "Match Whole Word", false }
            };
        }

        public void UpdateProjectRoot(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Warning: The directory '{directoryPath}' does not exist. Using current directory as fallback.");
                _projectRoot = Directory.GetCurrentDirectory();
            }
            else
            {
                _projectRoot = directoryPath;
            }
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();

            await Task.Run(() =>
            {
                if (!Directory.Exists(_projectRoot))
                {
                    Console.WriteLine($"Warning: The project root directory '{_projectRoot}' does not exist. Using current directory as fallback.");
                    _projectRoot = Directory.GetCurrentDirectory();
                }

                var files = GetRelevantFiles(_projectRoot);

                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var matches = PerformSearch(content, query);

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

        private MatchCollection PerformSearch(string content, string query)
        {
            RegexOptions options = RegexOptions.None;
            if (!_filters["Match Case"])
            {
                options |= RegexOptions.IgnoreCase;
            }

            string pattern = query;
            if (!_filters["Regex"])
            {
                pattern = Regex.Escape(pattern);
            }

            if (_filters["Match Whole Word"])
            {
                pattern = $@"\b{pattern}\b";
            }

            return Regex.Matches(content, pattern, options);
        }

        private IEnumerable<string> GetRelevantFiles(string root)
        {
            if (!Directory.Exists(root))
            {
                Console.WriteLine($"Warning: The directory '{root}' does not exist. Using current directory as fallback.");
                root = Directory.GetCurrentDirectory();
            }

            return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    var relativePath = Path.GetRelativePath(root, file);

                    return _includedExtensions.Contains(extension) &&
                           !_excludedDirectories.Any(dir =>
                               relativePath.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                               relativePath.Contains(Path.DirectorySeparatorChar + dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
                });
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

        public void UpdateFilter(string filterName, bool isActive)
        {
            if (_filters.ContainsKey(filterName))
            {
                _filters[filterName] = isActive;
            }
        }
    }
}