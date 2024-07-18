using meteor.Application.Interfaces;
using meteor.Application.Services;
using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models.Events;
using meteor.UI.ViewModels;

namespace meteor.IntegrationTests.UI.ViewModels;

public class EditorViewModelIntegrationTests : IDisposable
{
    private readonly ITextBufferService _textBufferService;
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly ICursorService _cursorService;
    private readonly ISelectionService _selectionService;
    private readonly IInputService _inputService;
    private readonly EditorViewModel _viewModel;
    private readonly ITextAnalysisService _textAnalysisService;

    public EditorViewModelIntegrationTests()
    {
        _textBufferService = new TextBufferService();
        _syntaxHighlighter = new SyntaxHighlighter();
        _cursorService = new CursorService(_textBufferService);
        _selectionService = new SelectionService();
        _textAnalysisService = new TextAnalysisService();
        _inputService = new InputService(_textBufferService, _cursorService, _textAnalysisService, _selectionService);

        _viewModel = new EditorViewModel(
            _textBufferService,
            _syntaxHighlighter,
            _selectionService,
            _inputService
        );
    }

    public void Dispose()
    {
        // Cleanup code if needed
    }

    [Fact]
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

    [Fact]
    public void Text_Set_UpdatesTextBufferAndTriggersHighlighting()
    {
        // Arrange
        var initialText = "Initial text";
        var newText = "New text";
        _textBufferService.ReplaceAll(initialText);

        // Act
        _viewModel.Text = newText;

        // Assert
        Assert.Equal(newText, _textBufferService.GetText());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [Fact]
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
        Assert.Equal(initialText + textToInsert, _textBufferService.GetText());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [Fact]
    public void DeleteText_UpdatesTextAndHighlighting()
    {
        // Arrange
        var initialText = "Initial text";
        _textBufferService.ReplaceAll(initialText);

        // Act
        _viewModel.DeleteText(0, 8);

        // Assert
        Assert.Equal("text", _textBufferService.GetText());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [Fact]
    public void OnPointerPressed_UpdatesCursorAndStartsSelection()
    {
        // Arrange
        var args = new PointerPressedEventArgs { Index = 5 };

        // Act
        _viewModel.OnPointerPressed(args);

        // Assert
        Assert.Equal(5, _cursorService.GetCursorPosition());
        // You might need to add a method to check if selection has started
    }

    [Fact]
    public void OnPointerMoved_UpdatesCursorAndSelection()
    {
        // Arrange
        var args = new PointerEventArgs { X = 10, Y = 20, Index = 15 };

        // Act
        _viewModel.OnPointerMoved(args);

        // Assert
        // You might need to add methods to check cursor and selection state
    }

    [Fact]
    public void OnPointerReleased_UpdatesSelection()
    {
        // Arrange
        var args = new PointerReleasedEventArgs { Index = 25 };

        // Act
        _viewModel.OnPointerReleased(args);

        // Assert
        // You might need to add a method to check selection state
    }

    [Fact]
    public void Text_Set_UpdatesTextBufferAndTriggersHighlighting_WithKeyword()
    {
        // Arrange
        var newText = "if (condition) { }";
        _textBufferService.ReplaceAll("");

        // Act
        _viewModel.Text = newText;

        // Assert
        Assert.Equal(newText, _textBufferService.GetText());
        Assert.NotEmpty(_viewModel.HighlightingResults);
    }

    [Fact]
    public void Text_Set_UpdatesTextBufferAndTriggersHighlighting_WithoutKeyword()
    {
        // Arrange
        var newText = "Just some plain text";
        _textBufferService.ReplaceAll("");

        // Act
        _viewModel.Text = newText;

        // Assert
        Assert.Equal(newText, _textBufferService.GetText());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [Fact]
    public void OnTextInput_InsertsTextAndUpdatesHighlighting_WithKeyword()
    {
        // Arrange
        _textBufferService.ReplaceAll("");
        _cursorService.SetCursorPosition(0);
        var args = new TextInputEventArgs { Text = "if" };

        // Act
        _viewModel.OnTextInput(args);

        // Assert
        Assert.Equal("if", _textBufferService.GetText());
        Assert.NotEmpty(_viewModel.HighlightingResults);
    }

    [Fact]
    public void OnTextInput_InsertsTextAndUpdatesHighlighting_WithoutKeyword()
    {
        // Arrange
        _textBufferService.ReplaceAll("");
        _cursorService.SetCursorPosition(0);
        var args = new TextInputEventArgs { Text = "hello" };

        // Act
        _viewModel.OnTextInput(args);

        // Assert
        Assert.Equal("hello", _textBufferService.GetText());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [Fact]
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

    [Fact]
    public void OnPointerPressed_SingleClick_UpdatesCursorAndStartsSelection()
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

    [Fact]
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

    [Fact]
    public void OnPointerPressed_TripleClick_SelectsLine()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello\nWorld");
        var args = new PointerPressedEventArgs { Index = 2 };

        // Act
        _viewModel.OnPointerPressed(args);
        _viewModel.OnPointerPressed(args);
        _viewModel.OnPointerPressed(args);

        // Assert
        Assert.Equal((0, 5), _selectionService.GetSelection());
        Assert.Equal(5, _cursorService.GetCursorPosition());
    }

    [Fact]
    public void OnPointerMoved_UpdatesSelectionAndCursor()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");
        _viewModel.OnPointerPressed(new PointerPressedEventArgs { Index = 0 });

        // Act
        _viewModel.OnPointerMoved(new PointerEventArgs { X = 50, Y = 10, Index = 5 });

        // Assert
        Assert.Equal((0, 5), _selectionService.GetSelection());
        Assert.Equal(11, _cursorService.GetCursorPosition());
    }

    [Fact]
    public void OnPointerReleased_FinalizesSelection()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello World");
        _viewModel.OnPointerPressed(new PointerPressedEventArgs { Index = 0 });
        _viewModel.OnPointerMoved(new PointerEventArgs { X = 50, Y = 10, Index = 5 });

        // Act
        _viewModel.OnPointerReleased(new PointerReleasedEventArgs { Index = 5 });

        // Assert
        Assert.Equal((0, 5), _selectionService.GetSelection());
    }

    [Fact]
    public void OnKeyDown_ArrowRight_MovesCursor()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(0);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Right));

        // Assert
        Assert.Equal(1, _cursorService.GetCursorPosition());
        Assert.Equal((0, 0), _selectionService.GetSelection());
    }

    [Fact]
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

    [Fact]
    public void OnKeyDown_Backspace_DeletesCharacterAndUpdatesCursor()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(5);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Backspace));

        // Assert
        Assert.Equal("Hell", _textBufferService.GetText());
        Assert.Equal(4, _cursorService.GetCursorPosition());
    }

    [Fact]
    public void OnKeyDown_Delete_DeletesCharacter()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(2);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Delete));

        // Assert
        Assert.Equal("Helo", _textBufferService.GetText());
        Assert.Equal(2, _cursorService.GetCursorPosition());
    }

    [Fact]
    public void OnKeyDown_Enter_InsertsNewline()
    {
        // Arrange
        _textBufferService.ReplaceAll("Hello");
        _cursorService.SetCursorPosition(5);

        // Act
        _viewModel.OnKeyDown(new KeyEventArgs(Key.Enter));

        // Assert
        Assert.Equal("Hello\n", _textBufferService.GetText());
        Assert.Equal(6, _cursorService.GetCursorPosition());
    }

    [Fact]
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