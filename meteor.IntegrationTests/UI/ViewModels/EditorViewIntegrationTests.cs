using System.Text;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using meteor.Application.Services;
using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Events;
using meteor.Core.Services;
using meteor.IntegrationTests.Mocks;
using meteor.UI.Services;
using meteor.UI.ViewModels;

namespace meteor.IntegrationTests.UI.ViewModels;

public class EditorViewModelIntegrationTests : IDisposable
{
    private readonly IClipboardService _clipboardService;
    private readonly ICursorService _cursorService;
    private readonly IEditorSizeCalculator _editorSizeCalculator;
    private readonly IInputService _inputService;
    private readonly ISelectionService _selectionService;
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly ITabService _tabService;
    private readonly ITextAnalysisService _textAnalysisService;
    private readonly ITextBufferService _textBufferService;
    private readonly ITextMeasurer _textMeasurer;
    private readonly EditorViewModel _viewModel;

    public EditorViewModelIntegrationTests()
    {
        _tabService = new TabService();
        _textBufferService = new TextBufferService();
        _syntaxHighlighter = new SyntaxHighlighter(_textBufferService);
        _cursorService = new CursorService(_tabService);
        _selectionService = new SelectionService();
        _textAnalysisService = new TextAnalysisService();
        _clipboardService = new MockClipboardService();
        _textMeasurer = new AvaloniaTextMeasurer(new Typeface("Consolas"), 13);
        _inputService = new InputService(_tabService, _cursorService, _textAnalysisService, _selectionService,
            _clipboardService, _textMeasurer);
        _editorSizeCalculator = new AvaloniaEditorSizeCalculator(_textMeasurer);

        _viewModel = new EditorViewModel(
            _textBufferService,
            _tabService,
            _syntaxHighlighter,
            _selectionService,
            _inputService,
            _cursorService,
            _editorSizeCalculator
        );

        _tabService.RegisterTab(0, _textBufferService); // Register initial tab with index 0
        _tabService.SwitchTab(0); // Set tab 0 as the active tab
    }

    public void Dispose()
    {
    }

    [AvaloniaFact]
    public void Text_Get_ReturnsTextFromTextBufferService()
    {
        // Arrange
        var expectedText = "Sample text";
        _textBufferService.ReplaceAll(expectedText);

        // Act
        var result = _viewModel.Text;

        // Assert
        Assert.Equal(expectedText, result);
    }

    [AvaloniaFact]
    public void Text_Set_UpdatesTextBufferAndTriggersHighlighting()
    {
        // Arrange
        var initialText = "Initial text";
        var newText = "New text";
        _textBufferService.ReplaceAll(initialText);

        // Act
        _viewModel.Text = newText;

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal(newText, sb.ToString());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void InsertText_UpdatesTextAndHighlighting()
    {
        // Arrange
        var initialText = "Initial";
        var textToInsert = " text";
        _textBufferService.ReplaceAll(initialText);
        _cursorService.SetCursorPosition(initialText.Length);

        // Act
        _viewModel.InsertText(initialText.Length, textToInsert);

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal(initialText + textToInsert, sb.ToString());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void DeleteText_UpdatesTextAndHighlighting()
    {
        // Arrange
        var initialText = "Initial text";
        _textBufferService.ReplaceAll(initialText);

        // Act
        _viewModel.DeleteText(0, 8);

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal("text", sb.ToString());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void OnPointerPressed_UpdatesCursorAndStartsSelection()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");
        var args = new PointerPressedEventArgs { Index = 5 };

        // Act
        _viewModel.OnPointerPressed(args);

        // Assert
        Assert.Equal(5, _cursorService.GetCursorPosition());
        Assert.Equal((5, 0), _selectionService.GetSelection());
    }

    [AvaloniaFact]
    public void OnPointerMoved_UpdatesCursorAndSelection()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");
        _viewModel.OnPointerPressed(new PointerPressedEventArgs { X = 0, Y = 0, Index = 0 });
        var args = new PointerEventArgs { X = 50, Y = 10, Index = 5, IsLeftButtonPressed = true };

        // Act
        _viewModel.OnPointerMoved(args);

        // Assert
        Assert.Equal(5, _viewModel.CursorPosition);
        Assert.Equal((0, 5), _viewModel.Selection);
    }

    [AvaloniaFact]
    public void Text_Set_UpdatesTextBufferAndTriggersHighlighting_WithKeyword()
    {
        // Arrange
        var newText = "if (condition) { }";
        _textBufferService.ReplaceAll("");

        // Act
        _viewModel.Text = newText;

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal(newText, sb.ToString());
        Assert.NotEmpty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void Text_Set_UpdatesTextBufferAndTriggersHighlighting_WithoutKeyword()
    {
        // Arrange
        var newText = "Just some plain text";
        _textBufferService.ReplaceAll("");

        // Act
        _viewModel.Text = newText;

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal(newText, sb.ToString());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void OnTextInput_InsertsTextAndUpdatesHighlighting_WithKeyword()
    {
        // Arrange
        _textBufferService.ReplaceAll("");
        _cursorService.SetCursorPosition(0);
        var args = new TextInputEventArgs { Text = "if" };

        // Act
        _viewModel.OnTextInput(args);

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal("if", sb.ToString());
        Assert.NotEmpty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void OnTextInput_InsertsTextAndUpdatesHighlighting_WithoutKeyword()
    {
        // Arrange
        _textBufferService.ReplaceAll("");
        _cursorService.SetCursorPosition(0);
        var args = new TextInputEventArgs { Text = "hello" };

        // Act
        _viewModel.OnTextInput(args);

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal("hello", sb.ToString());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void PropertyChanged_IsRaisedWhenTextChanges()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(EditorViewModel.Text)) propertyChangedRaised = true;
        };

        // Act
        _viewModel.Text = "New text";

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [AvaloniaFact]
    public void OnPointerPressed_SingleClick_UpdatesCursorAndStartsSelection()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");
        var args = new PointerPressedEventArgs { X = 50, Y = 10, Index = 5 };

        // Act
        _viewModel.OnPointerPressed(args);

        // Assert
        Assert.Equal(5, _viewModel.CursorPosition);
        Assert.Equal((5, 0), _viewModel.Selection);
    }

    [AvaloniaFact]
    public void OnPointerPressed_DoubleClick_SelectsWord()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");
        var args = new PointerPressedEventArgs { Index = 2 };

        // Act
        _viewModel.OnPointerPressed(args);
        _viewModel.OnPointerPressed(args);

        // Assert
        Assert.Equal((0, 5), _selectionService.GetSelection());
        Assert.Equal(5, _cursorService.GetCursorPosition());
    }

    [AvaloniaFact]
    public void OnPointerPressed_TripleClick_SelectsLine()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello\nWorld");
        var args = new PointerPressedEventArgs { X = 10, Y = 0, Index = 2 };

        // Act
        _viewModel.OnPointerPressed(args);
        _viewModel.OnPointerPressed(args);
        _viewModel.OnPointerPressed(args);

        // Assert
        Assert.Equal((0, 5), _viewModel.Selection); // Include newline character
        Assert.Equal(5, _viewModel.CursorPosition);
    }

    [AvaloniaFact]
    public void OnPointerMoved_UpdatesSelectionAndCursor()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");
        _viewModel.OnPointerPressed(new PointerPressedEventArgs { Index = 0 });

        // Act
        _viewModel.OnPointerMoved(new PointerEventArgs { X = 50, Y = 10, Index = 5, IsLeftButtonPressed = true });

        // Assert
        Assert.Equal((0, 5), _selectionService.GetSelection());
        Assert.Equal(5, _cursorService.GetCursorPosition());
    }

    [AvaloniaFact]
    public void OnKeyDown_ShiftArrowLeft_ExtendsSelectionBackwards()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(5);
        _selectionService.StartSelection(5);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Left, KeyModifiers.Shift));

        // Assert
        Assert.Equal(4, _viewModel.CursorPosition);
        Assert.Equal((4, 1), _viewModel.Selection);
    }

    [AvaloniaFact]
    public void OnKeyDown_CtrlA_SelectsAllText()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.A, KeyModifiers.Ctrl));

        // Assert
        Assert.Equal((0, 11), _viewModel.Selection);
        Assert.Equal(11, _viewModel.CursorPosition);
    }

    [AvaloniaFact]
    public void ClearSelection_ResetsSelectionToNegativeOne()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");
        _selectionService.SetSelection(0, 5);

        // Act
        _selectionService.ClearSelection();

        // Assert
        Assert.Equal((-1, 0), _selectionService.GetSelection());
    }

    [AvaloniaFact]
    public void OnPointerReleased_FinalizesSelection()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");
        _viewModel.OnPointerPressed(new PointerPressedEventArgs { X = 0, Y = 0, Index = 0 });
        _viewModel.OnPointerMoved(new PointerEventArgs { X = 50, Y = 10, Index = 5, IsLeftButtonPressed = true });

        // Act
        _viewModel.OnPointerReleased(new PointerReleasedEventArgs { X = 50, Y = 10, Index = 5 });

        // Assert
        Assert.Equal((0, 5), _selectionService.GetSelection());
        Assert.Equal(5, _cursorService.GetCursorPosition());
    }

    [AvaloniaFact]
    public void OnKeyDown_ArrowRight_MovesCursor()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(0);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Right));

        // Assert
        Assert.Equal(1, _cursorService.GetCursorPosition());
        Assert.Equal((-1, 0), _selectionService.GetSelection());
    }

    [AvaloniaFact]
    public void OnKeyDown_ShiftArrowRight_ExtendsSelection()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(0);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Right, KeyModifiers.Shift));

        // Assert
        Assert.Equal(1, _cursorService.GetCursorPosition());
        Assert.Equal((0, 1), _selectionService.GetSelection());
    }

    [AvaloniaFact]
    public void OnKeyDown_Backspace_DeletesCharacterAndUpdatesCursor()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(5);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Backspace));

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal("Hell", sb.ToString());
        Assert.Equal(4, _cursorService.GetCursorPosition());
    }

    [AvaloniaFact]
    public void OnKeyDown_Delete_DeletesCharacter()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(2);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Delete));

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal("Helo", sb.ToString());
        Assert.Equal(2, _cursorService.GetCursorPosition());
    }

    [AvaloniaFact]
    public void OnKeyDown_Enter_InsertsNewline()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(5);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Enter));

        // Assert
        var sb = new StringBuilder();
        _textBufferService.AppendTo(sb);
        Assert.Equal("Hello\n", sb.ToString());
        Assert.Equal(6, _cursorService.GetCursorPosition());
    }

    [AvaloniaFact]
    public void PropertyChanged_IsRaisedForSelectionChanges()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(EditorViewModel.Selection)) propertyChangedRaised = true;
        };

        // Act
        _viewModel.OnPointerPressed(new PointerPressedEventArgs { Index = 0 });
        _viewModel.OnPointerMoved(new PointerEventArgs { X = 50, Y = 10, Index = 5 });

        // Assert
        Assert.True(propertyChangedRaised);
    }
}