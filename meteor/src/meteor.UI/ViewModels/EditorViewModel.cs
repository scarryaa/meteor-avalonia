using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.EventArgs;

namespace meteor.UI.ViewModels;

public class EditorViewModel : IEditorViewModel
{
    private readonly ICursorManager _cursorManager;
    private readonly IInputManager _inputManager;
    private readonly ISelectionManager _selectionManager;
    private readonly IEditorConfig _config;
    private readonly ITextMeasurer _textMeasurer;
    private string _content;
    private int _lastSyncedVersion;

    public event EventHandler<ContentChangeEventArgs>? ContentChanged;
    public event EventHandler? SelectionChanged;

    public EditorViewModel(
        ITextBufferService textBufferService,
        ICursorManager cursorManager,
        IInputManager inputManager,
        ISelectionManager selectionManager,
        IEditorConfig config,
        ITextMeasurer textMeasurer)
    {
        TextBufferService = textBufferService;
        _cursorManager = cursorManager;
        _inputManager = inputManager;
        _selectionManager = selectionManager;
        _config = config;
        _textMeasurer = textMeasurer;

        _cursorManager.CursorPositionChanged += (_, _) => NotifyContentChanged();
        _selectionManager.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);

        _lastSyncedVersion = TextBufferService.GetDocumentVersion();
        _content = TextBufferService.GetContentSlice(0, TextBufferService.GetLength());
    }

    public ITextBufferService TextBufferService { get; }

    public int SelectionStart => _selectionManager.CurrentSelection.Start;
    public int SelectionEnd => _selectionManager.CurrentSelection.End;

    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                TextBufferService.Replace(0, TextBufferService.GetLength(), value);
                _content = value;
                NotifyContentChanged();
            }
        }
    }
    
    public int GetLineCount()
    {
        return TextBufferService.GetLineCount();
    }

    public int GetDocumentLength()
    {
        return TextBufferService.GetLength();
    }

    public int GetDocumentVersion()
    {
        return TextBufferService.GetDocumentVersion();
    }

    public double GetMaxLineWidth()
    {
        return TextBufferService.GetMaxLineWidth(_config.FontFamily, _config.FontSize);
    }

    public string GetContentSlice(int start, int end)
    {
        return TextBufferService.GetContentSlice(start, end);
    }

    public int GetCursorLine()
    {
        return _cursorManager.GetCursorLine();
    }

    public int GetCursorColumn()
    {
        return _cursorManager.GetCursorColumn();
    }

    public double GetCursorX()
    {
        var cursorLine = _cursorManager.GetCursorLine();
        var cursorColumn = _cursorManager.GetCursorColumn();
        var lineContent = TextBufferService.GetContentSlice(cursorLine, cursorLine);

        cursorColumn = Math.Min(cursorColumn, lineContent.Length);
        var textUpToCursor = lineContent.Substring(0, cursorColumn);

        return _textMeasurer.MeasureText(textUpToCursor, _config.FontFamily, _config.FontSize).Width;
    }
    
    public int CursorPosition => _cursorManager.Position;

    public void HandleKeyDown(KeyEventArgs e)
    {
        _inputManager.HandleKeyDown(e);
    }

    public void HandleTextInput(TextInputEventArgs e)
    {
        _inputManager.HandleTextInput(e);
    }

    public void StartSelection(int position)
    {
        _selectionManager.StartSelection(position);
        _cursorManager.SetPosition(position);
    }

    public void UpdateSelection(int position)
    {
        _selectionManager.ExtendSelection(position);
        _cursorManager.SetPosition(position);
    }

    public void EndSelection()
    {
    }

    public bool HasSelection()
    {
        return _selectionManager.HasSelection;
    }

    public void SetCursorPosition(int position)
    {
        _cursorManager.SetPosition(position);
    }

    public int GetLineStartOffset(int lineIndex)
    {
        return TextBufferService.GetLineStartOffset(lineIndex);
    }

    public int GetLineEndOffset(int lineIndex)
    {
        return TextBufferService.GetLineEndOffset(lineIndex);
    }

    private void NotifyContentChanged()
    {
        var currentVersion = TextBufferService.GetDocumentVersion();
        if (currentVersion != _lastSyncedVersion)
        {
            var newContent = TextBufferService.GetContentSlice(0, TextBufferService.GetLength());
            var changes = ComputeChanges(_content, newContent);
            _content = newContent;
            _lastSyncedVersion = currentVersion;
            ContentChanged?.Invoke(this, new ContentChangeEventArgs(changes));
        }
    }

    private IEnumerable<TextChange> ComputeChanges(string oldContent, string newContent)
    {
        yield return new TextChange(0, oldContent.Length, newContent.Length, newContent);
    }
}