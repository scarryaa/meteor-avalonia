using System;

namespace meteor.Core.Models
{
    public class SearchResult
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public DateTime LastModified { get; set; }
        public double Relevance { get; set; }

        public SearchResult()
        {
            Title = string.Empty;
            Description = string.Empty;
            Url = string.Empty;
            LastModified = DateTime.Now;
            Relevance = 0.0;
        }
    }
}
