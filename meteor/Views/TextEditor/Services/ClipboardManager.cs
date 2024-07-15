using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using meteor.Interfaces;

namespace meteor.Views.Services;

public class ClipboardManager(TextEditorViewModel viewModel, IClipboardService clipboardService)
{
    private TextEditorViewModel _viewModel = viewModel;

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

        await clipboardService.SetTextAsync(selectedText);
    }

    public async Task CutText()
    {
        await CopyText();
        await _viewModel.TextManipulator.DeleteSelectedTextAsync();
    }

    public async Task PasteText()
    {
        var text = await clipboardService.GetTextAsync();
        if (string.IsNullOrEmpty(text)) return;

        await Dispatcher.UIThread.InvokeAsync(async () => { await _viewModel.TextManipulator.InsertTextAsync(text); },
            DispatcherPriority.Background);
    }
}