using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.Services.Editor;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.EventArgs;

namespace meteor.UI.Services;

public class EditorInputHandler : IEditorInputHandler
{
    private readonly IEditorViewModel _viewModel;
    private readonly IScrollManager _scrollManager;
    private bool _isSelectAll;

    public EditorInputHandler(IEditorViewModel viewModel, IScrollManager scrollManager)
    {
        _viewModel = viewModel;
        _scrollManager = scrollManager;
    }

    public void HandleKeyDown(KeyEventArgs e)
    {
        _isSelectAll = false;

        var isModifierOrPageKey = IsModifierOrPageKey(e);

        if (IsSelectAllCommand(e))
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

    private bool IsModifierOrPageKey(KeyEventArgs e)
    {
        return e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
               e.Key == Key.LeftShift || e.Key == Key.RightShift ||
               e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
               e.Key == Key.PageUp || e.Key == Key.PageDown ||
               e.Modifiers.HasFlag(KeyModifiers.Alt) ||
               e.Modifiers.HasFlag(KeyModifiers.Control) ||
               e.Modifiers.HasFlag(KeyModifiers.Meta) ||
               e.Modifiers.HasFlag(KeyModifiers.Shift);
    }

    private bool IsSelectAllCommand(KeyEventArgs e)
    {
        return (e.Key == Key.A || e.Key == Key.C) &&
               (e.Modifiers.HasFlag(KeyModifiers.Control) || e.Modifiers.HasFlag(KeyModifiers.Meta));
    }
}