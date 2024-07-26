using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;

namespace meteor.Core.Services;

public class CursorManager : ICursorManager
{
    private readonly ITextBufferService _textBufferService;
    private readonly IEditorConfig _config;
    public int Position { get; private set; }
    private int _line;
    private int _column;
    private int _lastKnownLineStart;

    public event EventHandler? CursorPositionChanged;

    public CursorManager(ITextBufferService textBufferService, IEditorConfig config)
    {
        _config = config;
        _textBufferService = textBufferService;
        _line = 0;
        _column = 0;
        _lastKnownLineStart = 0;
    }

    public void MoveCursor(int offset)
    {
        SetPosition(Position + offset);
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

        if (Position < _lastKnownLineStart)
        {
            // If we moved backwards past the last known line start,
            // we need to search backwards for the previous line start
            var newLineStart = content.LastIndexOf('\n', Math.Max(0, Position - 1)) + 1;
            _line -= content.Substring(newLineStart, _lastKnownLineStart - newLineStart).Count(c => c == '\n');
            _lastKnownLineStart = newLineStart;
        }
        else if (Position > _lastKnownLineStart)
        {
            // If we moved forwards, we only need to count newlines from the last known position
            _line += content.Substring(_lastKnownLineStart, Position - _lastKnownLineStart).Count(c => c == '\n');
            _lastKnownLineStart = content.LastIndexOf('\n', Position - 1) + 1;
        }

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