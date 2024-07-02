using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Avalonia.Media;
using DiffPlex.DiffBuilder;
using meteor.Interfaces;
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
    private string _originalText = "";
    private string _text = "";
    private bool _isLoadingText;
    private DateTime _lastWriteTime;
    private const int AutoSaveInterval = 30000; // 30 seconds
    private const string BackupExtension = ".backup";
    private readonly ICursorPositionService _cursorPositionService;
    private readonly IUndoRedoManager<TextState> _undoRedoManager;
    private readonly IFileSystemWatcherFactory _fileSystemWatcherFactory;
    private Timer _autoSaveTimer;
    private readonly ITextBuffer _textBuffer;
    private readonly FontPropertiesViewModel _fontPropertiesViewModel;
    private readonly LineCountViewModel _lineCountViewModel;
    private readonly IClipboardService _clipboardService;
    private FileSystemWatcher? _fileWatcher;
    private readonly IAutoSaveService _autoSaveService;
    private bool _isClosing;
    private readonly IThemeService _themeService;
    private IBrush _foreground;
    private IBrush _background;
    private IBrush _borderBrush;
    private IBrush _closeButtonBackground;
    private IBrush _closeButtonForeground;
    private IBrush _dirtyIndicatorBrush;
    
    public event EventHandler? TextChanged;
    public event EventHandler? FileChangedExternally;
    public event EventHandler? TabClosed;
    public event EventHandler? InvalidateRequired;

    public ICommand CloseTabCommand { get; set; }
    public ICommand UndoCommand { get; set; }
    public ICommand RedoCommand { get; set; }
    public ICommand SaveCommand { get; set; }

    public TabViewModel(
        ICursorPositionService cursorPositionService,
        IUndoRedoManager<TextState> undoRedoManager,
        IFileSystemWatcherFactory fileSystemWatcherFactory,
        ITextBufferFactory textBufferFactory,
        FontPropertiesViewModel fontPropertiesViewModel,
        LineCountViewModel lineCountViewModel,
        IClipboardService clipboardService,
        IAutoSaveService autoSaveService,
        IThemeService themeService)
    {
        _themeService = themeService;
        _cursorPositionService = cursorPositionService;
        _undoRedoManager = undoRedoManager;
        _fileSystemWatcherFactory = fileSystemWatcherFactory;
        _textBuffer = textBufferFactory.Create();
        _fontPropertiesViewModel = fontPropertiesViewModel;
        _lineCountViewModel = lineCountViewModel;
        _clipboardService = clipboardService;
        _autoSaveService = autoSaveService;

        InitializeCommands();
        InitializeScrollableTextEditor();
        InitializeAutoSaveTimer();

        _themeService.ThemeChanged += OnThemeChanged;
        UpdateBrushes();
    }

    private void InitializeCommands()
    {
        UndoCommand = ReactiveCommand.Create(Undo, _undoRedoManager.WhenAnyValue(x => x.CanUndo));
        RedoCommand = ReactiveCommand.Create(Redo, _undoRedoManager.WhenAnyValue(x => x.CanRedo));
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, this.WhenAnyValue(x => x.IsDirty));
    }

    private void OnThemeChanged(object sender, EventArgs e)
    {
        UpdateBrushes();
    }

    private void UpdateBrushes()
    {
        Foreground = GetResourceBrush("Text");
        BorderBrush = GetResourceBrush("MainBorder");
        CloseButtonBackground = GetResourceBrush("TabCloseButtonBackground");
        CloseButtonForeground = GetResourceBrush("Text");

        UpdateDirtyIndicatorBrush(IsDirty);
        UpdateBackgroundBrush(IsSelected);
        OnInvalidateRequired();
    }

    private void UpdateBackgroundBrush(bool isSelected)
    {
        Background = isSelected ? GetResourceBrush("TextEditorBackground") : Brushes.Transparent;
    }

    private void UpdateDirtyIndicatorBrush(bool isDirty)
    {
        DirtyIndicatorBrush = isDirty ? GetResourceBrush("TabDirtyIndicator") : Brushes.Transparent;
    }

    private IBrush GetResourceBrush(string resourceKey)
    {
        return _themeService.GetResourceBrush(resourceKey);
    }

    private void InitializeScrollableTextEditor()
    {
        ScrollableTextEditorViewModel = new ScrollableTextEditorViewModel(
            _cursorPositionService,
            _fontPropertiesViewModel,
            _lineCountViewModel,
            _textBuffer,
            _clipboardService,
            _themeService);
        ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.LinesUpdated += OnTextBufferLinesUpdated;
        ScrollableTextEditorViewModel.TabViewModel = this;
    }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public bool IsNew { get; set; } = true;

    public IBrush DirtyIndicatorBrush
    {
        get => _dirtyIndicatorBrush;
        set => this.RaiseAndSetIfChanged(ref _dirtyIndicatorBrush, value);
    }

    public IBrush Foreground
    {
        get => _foreground;
        set => this.RaiseAndSetIfChanged(ref _foreground, value);
    }

    public IBrush CloseButtonForeground
    {
        get => _closeButtonForeground;
        set => this.RaiseAndSetIfChanged(ref _closeButtonForeground, value);
    }

    public IBrush CloseButtonBackground
    {
        get => _closeButtonBackground;
        set => this.RaiseAndSetIfChanged(ref _closeButtonBackground, value);
    }

    public IBrush Background
    {
        get => _background;
        set => this.RaiseAndSetIfChanged(ref _background, value);
    }

    public IBrush BorderBrush
    {
        get => _borderBrush;
        set => this.RaiseAndSetIfChanged(ref _borderBrush, value);
    }
    
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isSelected, value);
            UpdateBackgroundBrush(value);
        }
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
            if (_text != value)
            {
                OnTextChanged(_text, value);
                this.RaiseAndSetIfChanged(ref _text, value);
            }
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            this.RaiseAndSetIfChanged(ref _isDirty, value);
            UpdateDirtyIndicatorBrush(value);
        }
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

    private void InitializeAutoSaveTimer()
    {
        _autoSaveTimer = new Timer(AutoSaveInterval);
        _autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
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
            var initialState =
                new TextState(Text, (int)ScrollableTextEditorViewModel.TextEditorViewModel.CursorPosition);
            _undoRedoManager.AddState(initialState, "Loaded text");

            await _autoSaveService.InitializeAsync(filePath, Text);
            // Start auto-save timer when file is loaded
            _autoSaveTimer.Start();
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
            UpdateDirtyState();
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
                new TextState(currentText, (int)ScrollableTextEditorViewModel.TextEditorViewModel.CursorPosition),
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
        if (!IsNew && IsDirty) await _autoSaveService.SaveBackupAsync(Text);
    }

    public async Task SaveAsync()
    {
        if (IsDirty && !string.IsNullOrEmpty(FilePath))
        {
            try
            {
                await _autoSaveService.SaveAsync(Text);
                OriginalText = Text;
                UpdateDirtyState();
                IsNew = false;
                // Start auto-save timer when file is saved
                _autoSaveTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file: {ex.Message}");
                throw;
            }
        }
    }

    public async Task RestoreFromBackupAsync()
    {
        var backupContent = await _autoSaveService.RestoreFromBackupAsync();
        if (backupContent != null)
        {
            Text = backupContent;
            UpdateTextEditorBuffer();
            UpdateDirtyState();
        }
    }

    public async Task RestoreFromBackupAsync(string? backupId)
    {
        try
        {
            var backupContent = await _autoSaveService.RestoreFromBackupAsync(backupId);
            if (backupContent != null)
            {
                Text = backupContent;
                UpdateTextEditorBuffer();
                UpdateDirtyState();
            }
            else
            {
                // Handle case where no backup is found
                throw new InvalidOperationException("No backup found to restore.");
            }
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error restoring from backup: {ex.Message}");
            throw; // Rethrow to allow the caller to handle the error
        }
    }

    public bool HasBackup => _autoSaveService.HasBackup;

    private void SetupFileWatcher(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        var directoryName = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);

        if (directoryName == null || fileName == null)
            throw new ArgumentException("Invalid file path", nameof(filePath));

        _fileWatcher?.Dispose();
        _fileWatcher = _fileSystemWatcherFactory.Create(directoryName);
        _fileWatcher.Filter = fileName;
        _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            var newLastWriteTime = File.GetLastWriteTime(e.FullPath);
            if (newLastWriteTime != _lastWriteTime)
            {
                _lastWriteTime = newLastWriteTime;
                LoadTextAsync(e.FullPath);
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
        if (!string.IsNullOrEmpty(newValue))
            SetupFileWatcher(newValue);
    }

    private void OnTextChanged(string oldValue, string newValue)
    {
        if (!_isLoadingText)
        {
            var cursorPosition = ScrollableTextEditorViewModel.TextEditorViewModel.CursorPosition;
            var newState = new TextState(newValue, (int)cursorPosition);

            _undoRedoManager.AddState(newState, "Text change");
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

    public virtual void OnInvalidateRequired()
    {
        InvalidateRequired?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyTextChange(TextState newState)
    {
        if (newState == null)
        {
            Console.WriteLine("Error: newState is null.");
            return;
        }

        _isLoadingText = true;
        Text = newState.Text;
        OriginalText = newState.Text;
        ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.SetText(newState.Text);
        ScrollableTextEditorViewModel.TextEditorViewModel.CursorPosition = newState.CursorPosition;
        _isLoadingText = false;
        UpdateDirtyState();
        ScrollableTextEditorViewModel.TextEditorViewModel.UpdateLineStarts();
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

    public async Task CleanupBackupsAsync()
    {
        try
        {
            await _autoSaveService.CleanupAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up backups: {ex.Message}");
        }
    }

    public void Dispose()
    {
        ScrollableTextEditorViewModel.TextEditorViewModel.TextBuffer.LinesUpdated -= OnTextBufferLinesUpdated;
        _autoSaveTimer.Dispose();
        _fileWatcher?.Dispose();
    }
}