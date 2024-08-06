using meteor.Core.Enums;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.EventArgs;
using meteor.Core.Services;
using meteor.UI.Features.Editor.Services;

namespace meteor.UI.Features.Editor.ViewModels;

public class EditorViewModel : IEditorViewModel
{
    private readonly ICompletionProvider _completionProvider;
    private readonly Task<bool> _completionsInitTask;
    private readonly IEditorConfig _config;
    private readonly ICursorManager _cursorManager;
    private readonly IInputManager _inputManager;
    private readonly ISelectionManager _selectionManager;
    private readonly ITextMeasurer _textMeasurer;
    private string _content;
    private int _lastSyncedVersion;
    private readonly UndoRedoManager _undoRedoManager;
    private bool _isModularMode;
    private readonly IStatusBarService _statusBarService;
    private readonly VimService _vimService;

    public EditorViewModel(
        ITextBufferService textBufferService,
        ICursorManager cursorManager,
        IInputManager inputManager,
        ISelectionManager selectionManager,
        IEditorConfig config,
        ITextMeasurer textMeasurer,
        ICompletionProvider completionProvider,
        IStatusBarService statusBarService)
    {
        TextBufferService = textBufferService;
        _cursorManager = cursorManager;
        _inputManager = inputManager;
        _selectionManager = selectionManager;
        _config = config;
        _textMeasurer = textMeasurer;
        _completionProvider = completionProvider;
        _undoRedoManager = new UndoRedoManager();
        _isModularMode = false;
        _statusBarService = statusBarService;
        _vimService = new VimService(this, _cursorManager, _inputManager, textBufferService);

        _cursorManager.CursorPositionChanged += (_, _) => NotifyContentChanged();
        _selectionManager.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);

        _lastSyncedVersion = TextBufferService.GetDocumentVersion();
        _content = TextBufferService.GetContentSlice(0, TextBufferService.GetLength());
        _completionsInitTask = InitializeCompletionsAsync();

        UpdateStatusBar();
    }

    public event EventHandler<ContentChangeEventArgs>? ContentChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler<int>? CompletionIndexChanged;
    public event EventHandler? ModularModeChanged;

    public List<CompletionItem> CompletionItems { get; private set; }
    public bool IsCompletionActive { get; private set; }

    public bool IsModularMode
    {
        get => _isModularMode;
        set
        {
            if (_isModularMode != value)
            {
                _isModularMode = value;
                OnModularModeChanged();
            }
        }
    }

    public int SelectedCompletionIndex { get; set; }

    public async Task TriggerCompletionAsync()
    {
        if (!_completionsInitTask.IsCompleted)
        {
            CompletionItems = new List<CompletionItem>
                { new() { Text = "Loading...", Kind = CompletionItemKind.Text } };
            IsCompletionActive = true;
            CompletionIndexChanged?.Invoke(this, 0);

            await _completionsInitTask;
        }

        CompletionItems = (await _completionProvider.GetCompletionsAsync(CursorPosition)).ToList();

        if (CompletionItems.Count == 0)
        {
            CloseCompletion();
            return;
        }

        SelectedCompletionIndex = 0;
        IsCompletionActive = true;
        CompletionIndexChanged?.Invoke(this, SelectedCompletionIndex);
    }

    public void ApplySelectedCompletion()
    {
        if (IsCompletionActive && SelectedCompletionIndex >= 0 && SelectedCompletionIndex < CompletionItems.Count)
        {
            var selectedItem = CompletionItems[SelectedCompletionIndex];
            ApplyCompletion(selectedItem);
        }
    }

    public void MoveCompletionSelection(int delta)
    {
        if (IsCompletionActive)
        {
            SelectedCompletionIndex = (SelectedCompletionIndex + delta + CompletionItems.Count) % CompletionItems.Count;
            CompletionIndexChanged?.Invoke(this, SelectedCompletionIndex);
        }
    }

    public void CloseCompletion()
    {
        CompletionItems = null;
        IsCompletionActive = false;
    }

    private void OnModularModeChanged()
    {
        UpdateStatusBar();
        ModularModeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateStatusBar()
    {
        string modeText = _isModularMode ? _vimService.CurrentMode.ToString() : "Edit";
        _statusBarService.SetVimMode(modeText);
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
                var documentLength = TextBufferService.GetLength();
                TextBufferService.Replace(0, documentLength, value);
                _content = value;
                NotifyContentChanged();
            }
        }
    }

    public void HandleMouseSelection(int position, bool isShiftPressed)
    {
        _selectionManager.HandleMouseSelection(position, isShiftPressed);
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

    public void LoadContent(string content)
    {
        TextBufferService.LoadContent(content);
        Content = content;
    }

    public Point GetCursorPosition()
    {
        var cursorLine = _cursorManager.GetCursorLine();
        var cursorColumn = _cursorManager.GetCursorColumn();
        var lineContent = TextBufferService.GetContentSlice(cursorLine, cursorLine);

        cursorColumn = Math.Min(cursorColumn, lineContent.Length);
        var textUpToCursor = lineContent[..cursorColumn];

        var textMeasurements = string.IsNullOrEmpty(textUpToCursor)
            ? (Width: 0d, Height: _textMeasurer.GetLineHeight(_config.FontFamily, _config.FontSize))
            : _textMeasurer.MeasureText(textUpToCursor, _config.FontFamily, _config.FontSize);
        var cursorX = textMeasurements.Width;
        var cursorY = cursorLine * (textMeasurements.Height * _config.LineHeightMultiplier);

        return new Point(cursorX, cursorY);
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

        if (string.IsNullOrEmpty(lineContent) || lineContent == "\n") return 0;

        cursorColumn = Math.Min(cursorColumn, lineContent.Length);
        var textUpToCursor = lineContent[..cursorColumn];

        return _textMeasurer.MeasureText(textUpToCursor, _config.FontFamily, _config.FontSize).Width;
    }

    public void ToggleVimMode()
    {
        _isModularMode = !_isModularMode;
        OnModularModeChanged();
    }

    public int CursorPosition => _cursorManager.Position;

    public void HandleKeyDown(KeyEventArgs e)
    {
        if (_isModularMode)
        {
            _vimService.HandleKeyDown(e);
        }
        else
        {
            _inputManager.HandleKeyDown(e);
        }
    }

    public void HandleTextInput(TextInputEventArgs e)
    {
        if (_isModularMode && _vimService.CurrentMode == EditorMode.Insert)
        {
            _inputManager.HandleTextInput(e);
        }
        else if (!_isModularMode)
        {
            _inputManager.HandleTextInput(e);
        }
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

    private async Task<bool> InitializeCompletionsAsync()
    {
        try
        {
            await _completionProvider.GetCompletionsAsync(0);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void ApplyCompletion(CompletionItem item)
    {
        var wordStart = FindWordStart(Content, CursorPosition);
        var wordLength = CursorPosition - wordStart;

        TextBufferService.Replace(wordStart, wordLength, item.Text);
        SetCursorPosition(wordStart + item.Text.Length);

        CloseCompletion();
    }

    private int FindWordStart(string text, int position)
    {
        while (position > 0 && char.IsLetterOrDigit(text[position - 1])) position--;
        return position;
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
        yield return new TextChange(0, oldContent.Length, newContent.Length, newContent, oldContent);
    }

    public void GoToLineColumn(int line, int column)
    {
        _cursorManager.SetPosition(GetLineStartOffset(line) + column);
    }

    public void Undo()
    {
        var change = _undoRedoManager.Undo();
        if (change != null)
        {
            ApplyChange(change, true);
        }
    }

    public void Redo()
    {
        var change = _undoRedoManager.Redo();
        if (change != null)
        {
            ApplyChange(change, false);
        }
    }

    private void ApplyChange(TextChange change, bool isUndo)
    {
        int start = change.Offset;
        int oldLength = isUndo ? change.NewLength : change.OldLength;
        string newText = isUndo ? change.OldText : change.NewText;

        try
        {
            TextBufferService.Replace(start, oldLength, newText);
            _cursorManager.SetPosition(start + newText.Length);
            NotifyContentChanged();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"Error applying change: {ex.Message}");
        }
    }

    public void RecordChange(TextChange textChange)
    {
        _undoRedoManager.RecordChange(textChange);
    }

    public void PageUp()
    {
        _inputManager.HandleKeyDown(new KeyEventArgs(Key.PageUp, KeyModifiers.None));
    }

    public void PageDown()
    {
        _inputManager.HandleKeyDown(new KeyEventArgs(Key.PageDown, KeyModifiers.None));
    }
}