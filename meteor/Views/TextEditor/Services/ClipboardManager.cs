using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using meteor.Interfaces;
using meteor.ViewModels;

namespace meteor.Views.Services;

public class ClipboardManager
{
    private TextEditorViewModel _viewModel;
    private readonly IClipboardService _clipboardService;

    public ClipboardManager(TextEditorViewModel viewModel, IClipboardService clipboardService)
    {
        _viewModel = viewModel;
        _clipboardService = clipboardService;
    }

    public void UpdateViewModel(TextEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public async Task CopyText()
    {
        if (_viewModel.SelectionStart == _viewModel.SelectionEnd) return;

        var selectionStart = Math.Min(_viewModel.SelectionStart, _viewModel.SelectionEnd);
        var selectionEnd = Math.Max(_viewModel.SelectionStart, _viewModel.SelectionEnd);

        var selectedText = _viewModel.TextBuffer.GetText(selectionStart, selectionEnd - selectionStart);

        await _clipboardService.SetTextAsync(selectedText);
    }

    public async Task CutText()
    {
        await CopyText();
        await _viewModel.TextManipulator.DeleteSelectedTextAsync();
    }

    public async Task PasteText()
    {
        var text = await _clipboardService.GetTextAsync();
        if (string.IsNullOrEmpty(text)) return;

        await Dispatcher.UIThread.InvokeAsync(async () => { await _viewModel.TextManipulator.InsertTextAsync(text); },
            DispatcherPriority.Background);
    }
}