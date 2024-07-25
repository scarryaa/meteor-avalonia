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

    public event EventHandler CursorPositionChanged;

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
        var size = textMeasurer.MeasureText(lineText.Substring(0, _column), _config.FontFamily, _config.FontSize);
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
        Position = Math.Max(0, Math.Min(position, _textBufferService.GetLength()));
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
        _line = 0;
        _column = 0;

        for (var i = 0; i < Position; i++)
            if (content[i] == '\n')
            {
                _line++;
                _column = 0;
            }
            else
            {
                _column++;
            }

        _lastKnownLineStart = Position - _column;
    }

    private string GetCurrentLineText()
    {
        var content = _textBufferService.GetContent();
        var lineStart = content.LastIndexOf('\n', Position - 1) + 1;
        var lineEnd = content.IndexOf('\n', Position);
        if (lineEnd == -1) lineEnd = content.Length;
        return content.Substring(lineStart, lineEnd - lineStart);
    }
}