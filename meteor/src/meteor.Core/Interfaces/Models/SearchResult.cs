namespace meteor.Core.Models
{
    public class SearchResult
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string MatchedText { get; set; }
        public string SurroundingContext { get; set; }
        public DateTime LastModified { get; set; }
        public double Relevance { get; set; }
        public string Id { get; set; }

        public SearchResult()
        {
            FileName = string.Empty;
            FilePath = string.Empty;
            LineNumber = 0;
            MatchedText = string.Empty;
            SurroundingContext = string.Empty;
            LastModified = DateTime.Now;
            Relevance = 0.0;
            Id = Guid.NewGuid().ToString();
        }
    }
}
