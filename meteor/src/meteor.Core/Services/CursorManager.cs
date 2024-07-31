using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class CursorManager : ICursorManager
{
    private readonly IEditorConfig _config;
    private readonly ITextBufferService _textBufferService;
    private int _column;
    private int _lastKnownLineStart;
    private int _line;

    public CursorManager(ITextBufferService textBufferService, IEditorConfig config)
    {
        _config = config;
        _textBufferService = textBufferService;
        _line = 0;
        _column = 0;
        _lastKnownLineStart = 0;
    }

    public int Position { get; private set; }

    public event EventHandler? CursorPositionChanged;

    public void MoveCursor(int offset)
    {
        var newPosition = Position + offset;
        var documentLength = _textBufferService.GetLength();
        newPosition = Math.Max(0, Math.Min(newPosition, documentLength));
        SetPosition(newPosition);
    }

    public (double X, double Y) GetCursorPosition(ITextMeasurer textMeasurer, string text)
    {
        var lineText = GetCurrentLineText();
        var size = textMeasurer.MeasureText(lineText.Substring(0, Math.Min(_column, lineText.Length)),
            _config.FontFamily, _config.FontSize);
        return (size.Width, _line * textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize));
    }

    public int GetCursorLine()
    {
        return _line;
    }

    public int GetCursorColumn()
    {
        return _column;
    }

    public void SetPosition(int position)
    {
        var oldPosition = Position;
        var documentLength = _textBufferService.GetLength();
        Position = Math.Max(0, Math.Min(position, documentLength));
        if (Position != oldPosition)
        {
            UpdateLineAndColumn();
            OnCursorPositionChanged();
        }
    }

    protected virtual void OnCursorPositionChanged()
    {
        CursorPositionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateLineAndColumn()
    {
        var content = _textBufferService.GetContent();

        if (string.IsNullOrEmpty(content))
        {
            _line = 0;
            _column = 0;
            _lastKnownLineStart = 0;
            return;
        }

        // Ensure Position is within bounds
        Position = Math.Max(0, Math.Min(Position, content.Length));

        // Find the start of the current line
        _lastKnownLineStart = content.LastIndexOf('\n', Math.Max(0, Position - 1)) + 1;

        // Count the number of newlines from the beginning of the content to the current position
        _line = content.Substring(0, Position).Count(c => c == '\n');

        // Calculate the column
        _column = Position - _lastKnownLineStart;
    }

    private string GetCurrentLineText()
    {
        var content = _textBufferService.GetContent();
        if (string.IsNullOrEmpty(content)) return string.Empty;
        var lineEnd = content.IndexOf('\n', Position);
        if (lineEnd == -1) lineEnd = content.Length;
        return content.Substring(_lastKnownLineStart, lineEnd - _lastKnownLineStart);
    }
}