using meteor.Core.Enums.SyntaxHighlighting;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.SyntaxHighlighting;

namespace meteor.Application.Services;

public class SyntaxHighlighter : ISyntaxHighlighter
{
    private const int ChunkSize = 4096;

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "if", "else", "for", "while", "return"
    };

    private readonly char[] _buffer = new char[ChunkSize];

    private readonly ITextBufferService _textBufferService;

    public SyntaxHighlighter(ITextBufferService textBufferService)
    {
        _textBufferService = textBufferService;
    }

    public IEnumerable<SyntaxHighlightingResult> Highlight(string text)
    {
        var results = new List<SyntaxHighlightingResult>();
        var offset = 0;
        var bufferIndex = 0;

        _textBufferService.Iterate(c =>
        {
            _buffer[bufferIndex++] = c;
            if (bufferIndex == ChunkSize)
            {
                ProcessChunk(results, offset);
                offset += ChunkSize;
                bufferIndex = 0;
            }
        });

        if (bufferIndex > 0) ProcessChunk(results, offset, bufferIndex);

        return results;
    }

    private void ProcessChunk(List<SyntaxHighlightingResult> results, int offset, int length = ChunkSize)
    {
        HighlightKeywords(results, offset, length);
        HighlightComments(results, offset, length);
        HighlightStrings(results, offset, length);
    }

    private void HighlightKeywords(List<SyntaxHighlightingResult> results, int offset, int length)
    {
        var start = 0;
        while (start < length)
        {
            while (start < length && !char.IsLetter(_buffer[start])) start++;
            var end = start;
            while (end < length && char.IsLetterOrDigit(_buffer[end])) end++;
            if (end > start)
            {
                var word = new string(_buffer, start, end - start);
                if (Keywords.Contains(word))
                    results.Add(new SyntaxHighlightingResult
                    {
                        StartIndex = offset + start,
                        Length = end - start,
                        Type = SyntaxHighlightingType.Keyword
                    });
            }

            start = end;
        }
    }

    private void HighlightComments(List<SyntaxHighlightingResult> results, int offset, int length)
    {
        for (var i = 0; i < length - 1; i++)
            if (_buffer[i] == '/' && _buffer[i + 1] == '/')
            {
                var start = i;
                while (i < length && _buffer[i] != '\n') i++;
                results.Add(new SyntaxHighlightingResult
                {
                    StartIndex = offset + start,
                    Length = i - start,
                    Type = SyntaxHighlightingType.Comment
                });
            }
            else if (_buffer[i] == '/' && _buffer[i + 1] == '*')
            {
                var start = i;
                i += 2;
                while (i < length - 1 && !(_buffer[i] == '*' && _buffer[i + 1] == '/')) i++;
                if (i < length - 1)
                {
                    i += 2;
                    results.Add(new SyntaxHighlightingResult
                    {
                        StartIndex = offset + start,
                        Length = i - start,
                        Type = SyntaxHighlightingType.Comment
                    });
                }
            }
    }

    private void HighlightStrings(List<SyntaxHighlightingResult> results, int offset, int length)
    {
        var inString = false;
        var start = 0;
        for (var i = 0; i < length; i++)
            if (_buffer[i] == '"' && (i == 0 || _buffer[i - 1] != '\\'))
            {
                if (!inString)
                {
                    start = i;
                    inString = true;
                }
                else
                {
                    results.Add(new SyntaxHighlightingResult
                    {
                        StartIndex = offset + start,
                        Length = i - start + 1,
                        Type = SyntaxHighlightingType.String
                    });
                    inString = false;
                }
            }
    }
}