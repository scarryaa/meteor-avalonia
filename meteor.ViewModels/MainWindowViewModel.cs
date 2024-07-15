using System.Collections.ObjectModel;
using meteor.Core.Enums;
using meteor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace meteor.ViewModels;

public class MainWindowViewModel(
    ITextBufferFactory textBufferFactory,
    IDialogService dialogService,
    IClipboardService clipboardService,
    IUndoRedoManager<ITextBuffer> undoRedoManager,
    ICursorManager cursorManager,
    ISelectionHandler selectionHandler,
    ITextMeasurer textMeasurer,
    TextEditorViewModel textEditorViewModel,
    IServiceProvider serviceProvider)
{
    private TabViewModel? _selectedTab;
    private string _selectedPath;
    private readonly ITextBufferFactory _textBufferFactory = textBufferFactory;
    private readonly IClipboardService _clipboardService = clipboardService;
    private readonly IUndoRedoManager<ITextBuffer> _undoRedoManager = undoRedoManager;
    private readonly ICursorManager _cursorManager = cursorManager;
    private readonly ISelectionHandler _selectionHandler = selectionHandler;
    private readonly ITextMeasurer _textMeasurer = textMeasurer;

    private readonly ILogger<MainWindowViewModel> _logger =
        serviceProvider.GetService(typeof(ILogger<MainWindowViewModel>)) as ILogger<MainWindowViewModel> ??
        throw new InvalidOperationException("Logger not found");

    public TextEditorViewModel TextEditorViewModel { get; } = textEditorViewModel;

    public ObservableCollection<TabViewModel?> Tabs { get; } = new();

    public TabViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab != value)
            {
                _selectedTab = value;
                OnSelectedTabChanged();
            }
        }
    }

    public string SelectedPath
    {
        get => _selectedPath;
        set
        {
            if (_selectedPath != value)
            {
                _selectedPath = value;
                OnSelectedPathChanged();
            }
        }
    }

    public async Task NewTabAsync(string filePath = null)
    {
        var textEditorViewModel = serviceProvider.GetService(typeof(TextEditorViewModel)) as TextEditorViewModel;

        if (!string.IsNullOrEmpty(filePath))
            await LoadFileContentAsync(textEditorViewModel, filePath);

        var newTab = new TabViewModel
        {
            Title = filePath != null ? Path.GetFileName(filePath) : $"Untitled {Tabs.Count + 1}",
            TextEditorViewModel = textEditorViewModel,
            FilePath = filePath
        };

        Tabs.Add(newTab);
        SelectedTab = newTab;
    }

    public async Task CloseTabAsync(TabViewModel tab)
    {
        if (tab == null || !Tabs.Contains(tab))
            return;

        if (tab.IsDirty)
        {
            var saveResult = await ShowSaveConfirmationDialogAsync(tab);
            switch (saveResult)
            {
                case SaveConfirmationResult.Save:
                    await SaveFileAsync(tab);
                    break;
                case SaveConfirmationResult.DontSave:
                    break;
                case SaveConfirmationResult.Cancel:
                    return;
            }
        }

        Tabs.Remove(tab);
        UpdateSelectedTabAfterClose(tab);
    }

    public async Task OpenFileAsync(string filePath)
    {
        var existingTab = Tabs.FirstOrDefault(tab => tab.FilePath == filePath);
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        await NewTabAsync(filePath);
    }

    public async Task SaveFileAsync(TabViewModel tab)
    {
        if (tab == null || string.IsNullOrEmpty(tab.FilePath))
            return;

        try
        {
            File.WriteAllText(tab.FilePath, tab.TextEditorViewModel.TextBuffer.Text);
            tab.IsDirty = false;
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync($"Failed to save file: {ex.Message}");
        }
    }

    private async Task LoadFileContentAsync(TextEditorViewModel textEditorViewModel, string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            textEditorViewModel.TextBuffer.SetText(content);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync($"Failed to load file: {ex.Message}");
        }
    }

    private async Task<SaveConfirmationResult> ShowSaveConfirmationDialogAsync(TabViewModel tab)
    {
        var result = await dialogService.ShowConfirmationDialogAsync(
            $"Do you want to save changes to {tab.Title}?",
            "Save Changes",
            "Save",
            "Don't Save",
            "Cancel"
        );

        return result switch
        {
            DialogResult.Yes => SaveConfirmationResult.Save,
            DialogResult.No => SaveConfirmationResult.DontSave,
            _ => SaveConfirmationResult.Cancel
        };
    }

    private async Task ShowErrorDialogAsync(string message)
    {
        await dialogService.ShowErrorDialogAsync(message);
    }

    private void UpdateSelectedTabAfterClose(TabViewModel closedTab)
    {
        if (closedTab != SelectedTab)
            return;

        SelectedTab = Tabs.FirstOrDefault();
    }

    private void OnSelectedTabChanged()
    {
        // TODO Notify the view that the selected tab has changed
    }

    private void OnSelectedPathChanged()
    {
        // TODO Handle changes to the selected path (e.g., update file explorer)
    }
}