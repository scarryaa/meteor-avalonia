using System.Text;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
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
        _syntaxHighlighter = new SyntaxHighlighter(_tabService);
        _cursorService = new CursorService(_tabService);
        _selectionService = new SelectionService();
        _textAnalysisService = new TextAnalysisService();
        _clipboardService = new MockClipboardService();
        _textMeasurer = new AvaloniaTextMeasurer(new Typeface("Consolas"), 13);
        _inputService = new InputService(_tabService, _cursorService, _textAnalysisService, _selectionService,
            _clipboardService, _textMeasurer);
        _editorSizeCalculator = new AvaloniaEditorSizeCalculator(_textMeasurer);

        // Initialize the tab service and set up tabs
        _tabService.CloseAllTabs();
        var tab = _tabService.AddTab(_textBufferService);
        _tabService.SwitchTab(tab.Index);
        
        _viewModel = new EditorViewModel(
            _tabService.GetActiveTextBufferService(),
            _tabService,
            _syntaxHighlighter,
            _selectionService,
            _inputService,
            _cursorService,
            _editorSizeCalculator,
            _textMeasurer
        );
    }

    public void Dispose()
    {
        _tabService.CloseAllTabs(); // Clean up tabs
    }

    [AvaloniaFact]
    public void Text_Get_ReturnsTextFromTextBufferService()
    {
        // Arrange
        var expectedText = "Sample text";
        _viewModel.Text = expectedText;

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
        _viewModel.Text = initialText;

        // Act
        _viewModel.Text = newText;

        // Assert
        var sb = new StringBuilder();
        _viewModel.TextBufferService.AppendTo(sb);
        Assert.Equal(newText, sb.ToString());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void DeleteText_UpdatesTextAndHighlighting()
    {
        // Arrange
        var initialText = "Initial text";
        _viewModel.Text = initialText;

        // Act
        _viewModel.DeleteText(0, 8);

        // Assert
        var sb = new StringBuilder();
        _viewModel.TextBufferService.AppendTo(sb);
        Assert.Equal("text", sb.ToString());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void OnPointerPressed_UpdatesCursorAndStartsSelection()
    {
        // Arrange
        _viewModel.Text = "Hello World";
        var args = new PointerPressedEventArgs { Index = 5 };

        // Act
        _viewModel.OnPointerPressed(args);

        // Assert
        Assert.Equal(5, _viewModel.CursorPosition);
        Assert.Equal((5, 0), _viewModel.Selection);
    }

    [AvaloniaFact]
    public void OnPointerMoved_UpdatesCursorAndSelection()
    {
        // Arrange
        _viewModel.Text = "Hello World";
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
        _viewModel.Text = "";

        // Act
        _viewModel.Text = newText;

        // Assert
        var sb = new StringBuilder();
        _viewModel.TextBufferService.AppendTo(sb);
        Assert.Equal(newText, sb.ToString());
        Assert.NotEmpty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void Text_Set_UpdatesTextBufferAndTriggersHighlighting_WithoutKeyword()
    {
        // Arrange
        var newText = "Just some plain text";
        _viewModel.Text = "";

        // Act
        _viewModel.Text = newText;

        // Assert
        var sb = new StringBuilder();
        _viewModel.TextBufferService.AppendTo(sb);
        Assert.Equal(newText, sb.ToString());
        Assert.Empty(_viewModel.HighlightingResults);
    }

    [AvaloniaFact]
    public void OnTextInput_InsertsTextAndUpdatesHighlighting_WithoutKeyword()
    {
        // Arrange
        _viewModel.Text = "";
        var args = new TextInputEventArgs { Text = "hello" };

        // Act
        _viewModel.OnTextInput(args);

        // Assert
        var sb = new StringBuilder();
        _viewModel.TextBufferService.AppendTo(sb);
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
        _viewModel.Text = "Hello World";
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
        _viewModel.Text = "Hello World";
        var args = new PointerPressedEventArgs { Index = 2 };

        // Act
        _viewModel.OnPointerPressed(args);
        _viewModel.OnPointerPressed(args);

        // Assert
        Assert.Equal((0, 5), _viewModel.Selection);
        Assert.Equal(5, _viewModel.CursorPosition);
    }

    [AvaloniaFact]
    public void OnPointerPressed_TripleClick_SelectsLine()
    {
        // Arrange
        _viewModel.Text = "Hello\nWorld";
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
        _viewModel.Text = "Hello World";
        _viewModel.OnPointerPressed(new PointerPressedEventArgs { Index = 0 });

        // Act
        _viewModel.OnPointerMoved(new PointerEventArgs { X = 50, Y = 10, Index = 5, IsLeftButtonPressed = true });

        // Assert
        Assert.Equal((0, 5), _viewModel.Selection);
        Assert.Equal(5, _viewModel.CursorPosition);
    }
}
