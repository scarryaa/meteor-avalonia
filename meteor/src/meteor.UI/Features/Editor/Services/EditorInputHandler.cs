using meteor.Core.Enums;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.EventArgs;

namespace meteor.UI.Features.Editor.Services;

public class EditorInputHandler : IEditorInputHandler
{
    private readonly IModifierKeyHandler _modifierKeyHandler;
    private readonly IScrollManager _scrollManager;
    private readonly ISelectAllCommandHandler _selectAllCommandHandler;
    private bool _isSelectAll;

    public EditorInputHandler(
        IScrollManager scrollManager,
        IModifierKeyHandler modifierKeyHandler,
        ISelectAllCommandHandler selectAllCommandHandler)
    {
        _scrollManager = scrollManager;
        _modifierKeyHandler = modifierKeyHandler;
        _selectAllCommandHandler = selectAllCommandHandler;
    }

    public void HandleKeyDown(IEditorViewModel viewModel, KeyEventArgs e)
    {
        _isSelectAll = false;

        var isModifierOrPageKey = _modifierKeyHandler.IsModifierOrPageKey(e);

        if (_selectAllCommandHandler.IsSelectAllCommand(e))
            _isSelectAll = true;

        switch (e.Key)
        {
            case Key.PageUp:
                _scrollManager.PageUp();
                e.Handled = true;
                break;
            case Key.PageDown:
                _scrollManager.PageDown();
                e.Handled = true;
                break;
            default:
                viewModel.HandleKeyDown(e);
                break;
        }

        if (!isModifierOrPageKey && !_isSelectAll)
            _scrollManager.EnsureLineIsVisible(viewModel.GetCursorLine(), viewModel.GetCursorX(),
                viewModel.GetLineCount(), viewModel.HasSelection());
    }

    public void HandleTextInput(IEditorViewModel viewModel, TextInputEventArgs e)
    {
        viewModel.HandleTextInput(e);

        if (!_isSelectAll)
            _scrollManager.EnsureLineIsVisible(viewModel.GetCursorLine(),
                viewModel.GetCursorX(), viewModel.GetLineCount());
    }
}