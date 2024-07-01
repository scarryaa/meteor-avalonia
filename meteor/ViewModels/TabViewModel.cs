using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using DiffPlex.DiffBuilder;
using meteor.Models;
using ReactiveUI;
using File = System.IO.File;

namespace meteor.ViewModels;

public class TabViewModel : ViewModelBase, IDisposable
{
    private string _title = "Untitled";
    private bool _isSelected;
    private ScrollableTextEditorViewModel _scrollableTextEditorViewModel;
    private string? _filePath;
    private bool _isDirty;
    private bool _isTemporary;
    private double _savedVerticalOffset;
    private double _savedHorizontalOffset;
    private string _originalText = string.Empty;
    private string _text = string.Empty;
    private bool _isLoadingText;
    private readonly UndoRedoManager<TextState> _undoRedoManager;
    private readonly Timer _autoSaveTimer;
    private FileSystemWatcher? _fileWatcher;
    private DateTime _lastWriteTime;
    private const int AutoSaveInterval = 30000; // 30 seconds

    public event EventHandler? TextChanged;
    public event EventHandler? FileChangedExternally;

    public ICommand CloseTabCommand { get; set; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand SaveCommand { get; }

    public TabViewModel()
    {
        _undoRedoManager = new UndoRedoManager<TextState>(new TextState(string.Empty, 0));
        UndoCommand = ReactiveCommand.Create(Undo, _undoRedoManager.WhenAnyValue(x => x.CanUndo));
        RedoCommand = ReactiveCommand.Create(Redo, _undoRedoManager.WhenAnyValue(x => x.CanRedo));
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, this.WhenAnyValue(x => x.IsDirty));
        CloseTabCommand = ReactiveCommand.Create(() =>
        {
            /* Implement tab closing logic */
        });

        _autoSaveTimer = new Timer(AutoSaveInterval);
        _autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
    }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public bool IsNew { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public bool IsTemporary
    {
        get => _isTemporary;
        set => this.RaiseAndSetIfChanged(ref _isTemporary, value);
    }

    public string OriginalText
    {
        get => _originalText;
        set => this.RaiseAndSetIfChanged(ref _originalText, value);
    }

    public ScrollableTextEditorViewModel ScrollableTextEditorViewModel
    {
        get => _scrollableTextEditorViewModel;
        set
        {
            OnScrollableTextEditorViewModelChanged(_scrollableTextEditorViewModel, value);
            this.RaiseAndSetIfChanged(ref _scrollableTextEditorViewModel, value);
        }
    }

    public string? FilePath
    {
        get => _filePath;
        set
        {
            OnFilePathChanged(_filePath, value);
            this.RaiseAndSetIfChanged(ref _filePath, value);
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            OnTextChanged(_text, value);
            this.RaiseAndSetIfChanged(ref _text, value);
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    public double SavedVerticalOffset
    {
        get => _savedVerticalOffset;
        set => this.RaiseAndSetIfChanged(ref _savedVerticalOffset, value);
    }

    public double SavedHorizontalOffset
    {
        get => _savedHorizontalOffset;
        set => this.RaiseAndSetIfChanged(ref _savedHorizontalOffset, value);
    }

    public async Task LoadTextAsync(string filePath)
    {
        _isLoadingText = true;
        try
        {
            using var reader = new StreamReader(filePath, DetectEncoding(filePath));
            OriginalText = await reader.ReadToEndAsync();
            Text = OriginalText;
            UpdateTextEditorBuffer();

            FilePath = filePath;
            _lastWriteTime = File.GetLastWriteTime(filePath);
            IsNew = false;
            _undoRedoManager.Clear();
        }
        finally
        {
            _isLoadingText = false;
        }
    }

    public void Undo()
    {
        if (_undoRedoManager.CanUndo)
        {
            var (undoneState, description) = _undoRedoManager.Undo();
            ApplyTextChange(undoneState);
        }
    }

    public void Redo()
    {
        if (_undoRedoManager.CanRedo)
        {
            var (redoneState, description) = _undoRedoManager.Redo();
            ApplyTextChange(redoneState);
        }
    }

    private void OnTextBufferLinesUpdated(object? sender, EventArgs e)
    {
        if (!_isLoadingText)
        {
            var currentText = ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.Text;
            _undoRedoManager.AddState(
                new TextState(currentText, ScrollableTextEditorViewModel.TextEditorViewModel.CursorPosition),
                "Text buffer update");
            Text = currentText;
        }
    }

    private void UpdateDirtyState()
    {
        var diff = InlineDiffBuilder.Diff(OriginalText, Text);
        IsDirty = diff.HasDifferences;
    }

    private static Encoding DetectEncoding(string filePath)
    {
        using var reader = new StreamReader(filePath, Encoding.ASCII, true);
        reader.Peek();
        return reader.CurrentEncoding;
    }

    private async void AutoSaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!IsNew)
            await SaveAsync();
    }

    public async Task SaveAsync()
    {
        if (IsDirty && !string.IsNullOrEmpty(FilePath))
        {
            await File.WriteAllTextAsync(FilePath, Text);
            OriginalText = Text;
            UpdateDirtyState();
            _lastWriteTime = File.GetLastWriteTime(FilePath);
            IsNew = false;
        }
    }

    private void SetupFileWatcher(string filePath)
    {
        _fileWatcher?.Dispose();

        var directory = Path.GetDirectoryName(filePath);
        if (directory == null) return;

        _fileWatcher = new FileSystemWatcher(directory)
        {
            Filter = Path.GetFileName(filePath),
            NotifyFilter = NotifyFilters.LastWrite
        };

        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            var newLastWriteTime = File.GetLastWriteTime(e.FullPath);
            if (newLastWriteTime != _lastWriteTime)
            {
                _lastWriteTime = newLastWriteTime;
                await LoadTextAsync(e.FullPath);
                FileChangedExternally?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void OnScrollableTextEditorViewModelChanged(ScrollableTextEditorViewModel oldValue,
        ScrollableTextEditorViewModel newValue)
    {
        if (oldValue != null)
        {
            oldValue.TextEditorViewModel.TextBuffer.LinesUpdated -= OnTextBufferLinesUpdated;
            oldValue.TabViewModel = null;
        }

        if (newValue != null)
        {
            newValue.TextEditorViewModel.TextBuffer.LinesUpdated += OnTextBufferLinesUpdated;
            newValue.TabViewModel = this;
        }
    }

    private void OnFilePathChanged(string? oldValue, string? newValue)
    {
        if (newValue != null)
            SetupFileWatcher(newValue);
    }

    private void OnTextChanged(string oldValue, string newValue)
    {
        if (oldValue != newValue)
        {
            if (!_isLoadingText)
            {
                var cursorPosition = ScrollableTextEditorViewModel.TextEditorViewModel.CursorPosition;
                _undoRedoManager.AddState(new TextState(newValue, cursorPosition), "Text change");
                UpdateDirtyState();
                if (IsTemporary) IsTemporary = false;
                if (!IsNew)
                {
                    _autoSaveTimer.Stop();
                    _autoSaveTimer.Start();
                }
            }

            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ApplyTextChange(TextState newState)
    {
        _isLoadingText = true;
        Text = newState.Text;
        ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.SetText(newState.Text);
        ScrollableTextEditorViewModel.TextEditorViewModel.CursorPosition = newState.CursorPosition;
        _isLoadingText = false;
        UpdateDirtyState();
        ScrollableTextEditorViewModel.TextEditorViewModel.OnInvalidateRequired();
    }

    private void UpdateTextEditorBuffer()
    {
        ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.Clear();
        ScrollableTextEditorViewModel.TextEditorViewModel.LineCache.Clear();
        ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.SetText(Text);
        ScrollableTextEditorViewModel.TextEditorViewModel.OnInvalidateRequired();
        ScrollableTextEditorViewModel.GutterViewModel.OnInvalidateRequired();
    }

    public void Dispose()
    {
        _scrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.LinesUpdated -= OnTextBufferLinesUpdated;
        _autoSaveTimer.Dispose();
        _fileWatcher?.Dispose();
    }
}