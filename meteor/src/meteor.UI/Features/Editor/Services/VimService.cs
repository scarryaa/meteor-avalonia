using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.EventArgs;
using meteor.UI.Features.Editor.ViewModels;

namespace meteor.UI.Features.Editor.Services;

public class VimService
{
    private readonly EditorViewModel _editorViewModel;
    private readonly ICursorManager _cursorManager;
    private readonly IInputManager _inputManager;
    private readonly ITextBufferService _textBufferService;
    private EditorMode _currentMode;
    private string _commandBuffer;

    public VimService(EditorViewModel editorViewModel, ICursorManager cursorManager, IInputManager inputManager, ITextBufferService textBufferService)
    {
        _editorViewModel = editorViewModel;
        _cursorManager = cursorManager;
        _inputManager = inputManager;
        _textBufferService = textBufferService;
        _currentMode = EditorMode.Normal;
        _commandBuffer = string.Empty;
    }

    public EditorMode CurrentMode => _currentMode;

    public void HandleKeyDown(KeyEventArgs e)
    {
        switch (_currentMode)
        {
            case EditorMode.Normal:
                HandleNormalModeKeyDown(e);
                break;
            case EditorMode.Insert:
                HandleInsertModeKeyDown(e);
                break;
            case EditorMode.Visual:
                HandleVisualModeKeyDown(e);
                break;
            case EditorMode.Command:
                HandleCommandModeKeyDown(e);
                break;
        }
    }

    private void HandleNormalModeKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.H:
                _cursorManager.MoveCursor(-1);
                break;
            case Key.J:
                _cursorManager.MoveCursor(_textBufferService.GetContentSlice(_cursorManager.Position, _cursorManager.Position + 1).Length);
                break;
            case Key.K:
                _cursorManager.MoveCursor(-_textBufferService.GetContentSlice(_cursorManager.Position - 1, _cursorManager.Position).Length);
                break;
            case Key.L:
                _cursorManager.MoveCursor(1);
                break;
            case Key.I:
                EnterInsertMode();
                break;
            case Key.A:
                _cursorManager.MoveCursor(1);
                EnterInsertMode();
                break;
            case Key.V:
                EnterVisualMode();
                break;
            case Key.D:
                if (e.Modifiers.HasFlag(KeyModifiers.Control))
                {
                    _editorViewModel.PageDown();
                }
                else
                {
                    DeleteLine();
                }
                break;
            case Key.U:
                if (e.Modifiers.HasFlag(KeyModifiers.Control))
                {
                    _editorViewModel.PageUp();
                }
                break;
            case Key.X:
                DeleteCharacter();
                break;
            case Key.O:
                InsertNewLineBelow();
                break;
            case Key.Semicolon:
                EnterCommandMode();
                break;
        }
    }

    private void HandleInsertModeKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ExitInsertMode();
        }
        else
        {
            _inputManager.HandleKeyDown(e);
        }
    }

    private void HandleVisualModeKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                ExitVisualMode();
                break;
            case Key.H:
                _editorViewModel.UpdateSelection(_cursorManager.Position - 1);
                _cursorManager.MoveCursor(-1);
                break;
            case Key.J:
                var nextLineLength = _textBufferService.GetContentSlice(_cursorManager.Position, _cursorManager.Position + 1).Length;
                _editorViewModel.UpdateSelection(_cursorManager.Position + nextLineLength);
                _cursorManager.MoveCursor(nextLineLength);
                break;
            case Key.K:
                var prevLineLength = _textBufferService.GetContentSlice(_cursorManager.Position - 1, _cursorManager.Position).Length;
                _editorViewModel.UpdateSelection(_cursorManager.Position - prevLineLength);
                _cursorManager.MoveCursor(-prevLineLength);
                break;
            case Key.L:
                _editorViewModel.UpdateSelection(_cursorManager.Position + 1);
                _cursorManager.MoveCursor(1);
                break;
            case Key.D:
                DeleteSelection();
                ExitVisualMode();
                break;
            case Key.Y:
                YankSelection();
                ExitVisualMode();
                break;
        }
    }

    private void HandleCommandModeKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ExecuteCommand(_commandBuffer);
            ExitCommandMode();
        }
        else if (e.Key == Key.Escape)
        {
            ExitCommandMode();
        }
        else
        {
            _commandBuffer += e.Key.ToString().ToLower();
            _editorViewModel.UpdateStatusBar();
        }
    }

    private void EnterInsertMode()
    {
        _currentMode = EditorMode.Insert;
        _editorViewModel.UpdateStatusBar();
    }

    private void ExitInsertMode()
    {
        _currentMode = EditorMode.Normal;
        _cursorManager.MoveCursor(-1);
        _editorViewModel.UpdateStatusBar();
    }

    private void EnterVisualMode()
    {
        _currentMode = EditorMode.Visual;
        _editorViewModel.UpdateStatusBar();
        _editorViewModel.StartSelection(_cursorManager.Position);
    }

    private void ExitVisualMode()
    {
        _currentMode = EditorMode.Normal;
        _editorViewModel.UpdateStatusBar();
        _editorViewModel.EndSelection();
    }

    private void EnterCommandMode()
    {
        _currentMode = EditorMode.Command;
        _commandBuffer = string.Empty;
        _editorViewModel.UpdateStatusBar();
    }

    private void ExitCommandMode()
    {
        _currentMode = EditorMode.Normal;
        _commandBuffer = string.Empty;
        _editorViewModel.UpdateStatusBar();
    }

    private void DeleteLine()
    {
        var lineStart = _textBufferService.GetLineStartOffset(_cursorManager.Position);
        var lineEnd = _textBufferService.GetLineEndOffset(_cursorManager.Position);
        _textBufferService.DeleteText(lineStart, lineEnd - lineStart);
    }

    private void DeleteCharacter()
    {
        _textBufferService.DeleteText(_cursorManager.Position, 1);
    }

    private void InsertNewLineBelow()
    {
        var lineEnd = _textBufferService.GetLineEndOffset(_cursorManager.Position);
        _textBufferService.InsertText(lineEnd, "\n");
        _cursorManager.SetPosition(lineEnd + 1);
        EnterInsertMode();
    }

    private void DeleteSelection()
    {

    }

    private void YankSelection()
    {

    }

    private void ExecuteCommand(string command)
    {

    }
}