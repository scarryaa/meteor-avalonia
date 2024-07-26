using meteor.Core.Enums;
using meteor.Core.Interfaces.Commands;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.EventArgs;

namespace meteor.UI.Services;

public class EditorInputHandler : IEditorInputHandler
{
    private readonly IEditorViewModel _viewModel;
    private readonly IScrollManager _scrollManager;
    private readonly IModifierKeyHandler _modifierKeyHandler;
    private readonly ISelectAllCommandHandler _selectAllCommandHandler;
    private bool _isSelectAll;

    public EditorInputHandler(
        IEditorViewModel viewModel,
        IScrollManager scrollManager,
        IModifierKeyHandler modifierKeyHandler,
        ISelectAllCommandHandler selectAllCommandHandler)
    {
        _viewModel = viewModel;
        _scrollManager = scrollManager;
        _modifierKeyHandler = modifierKeyHandler;
        _selectAllCommandHandler = selectAllCommandHandler;
    }

    public void HandleKeyDown(KeyEventArgs e)
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
                _viewModel.HandleKeyDown(e);
                break;
        }

        if (!isModifierOrPageKey && !_isSelectAll)
            _scrollManager.EnsureLineIsVisible(_viewModel.GetCursorLine(), _viewModel.GetCursorX(),
                _viewModel.HasSelection());
    }

    public void HandleTextInput(TextInputEventArgs e)
    {
        _viewModel.HandleTextInput(e);

        if (!_isSelectAll)
            _scrollManager.EnsureLineIsVisible(_viewModel.GetCursorLine(), _viewModel.GetCursorX());
    }
}