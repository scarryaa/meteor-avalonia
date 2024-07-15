using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Events;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace meteor.ViewModels.Tests;

public class TextEditorViewModelTests
{
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly Mock<IClipboardService> _mockClipboardService;
    private readonly Mock<IUndoRedoManager<ITextBuffer>> _mockUndoRedoManager;
    private readonly Mock<ICursorManager> _mockCursorManager;
    private readonly Mock<ISelectionHandler> _mockSelectionHandler;
    private readonly Mock<ITextMeasurer> _mockTextMeasurer;
    private readonly Mock<ILineCountViewModel> _mockLineCountViewModel;
    private readonly Mock<IGutterViewModel> _mockGutterViewModel;
    private readonly Mock<ILogger<TextEditorViewModel>> _mockLogger;
    private readonly Mock<IEventAggregator> _mockEventAggregator;

    private readonly TextEditorViewModel _viewModel;

    public TextEditorViewModelTests()
    {
        _mockTextBuffer = new Mock<ITextBuffer>();
        _mockClipboardService = new Mock<IClipboardService>();
        _mockUndoRedoManager = new Mock<IUndoRedoManager<ITextBuffer>>();
        _mockCursorManager = new Mock<ICursorManager>();
        _mockSelectionHandler = new Mock<ISelectionHandler>();
        _mockTextMeasurer = new Mock<ITextMeasurer>();
        _mockLineCountViewModel = new Mock<ILineCountViewModel>();
        _mockGutterViewModel = new Mock<IGutterViewModel>();
        _mockLogger = new Mock<ILogger<TextEditorViewModel>>();
        _mockEventAggregator = new Mock<IEventAggregator>();

        _mockTextMeasurer.Setup(tm => tm.GetLineHeight(It.IsAny<double>(), It.IsAny<string>())).Returns(15);

        _viewModel = new TextEditorViewModel(
            _mockTextBuffer.Object,
            _mockClipboardService.Object,
            _mockUndoRedoManager.Object,
            _mockCursorManager.Object,
            _mockSelectionHandler.Object,
            _mockTextMeasurer.Object,
            _mockLineCountViewModel.Object,
            _mockGutterViewModel.Object,
            _mockLogger.Object,
            _mockEventAggregator.Object
        );
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        Assert.Equal("Consolas", _viewModel.FontFamily);
        Assert.Equal(13, _viewModel.FontSize);
        Assert.Equal(15, _viewModel.LineHeight);
        Assert.NotNull(_viewModel.LineCountViewModel);
        Assert.NotNull(_viewModel.GutterViewModel);
    }

    [Fact]
    public void Constructor_SubscribesToEvents()
    {
        _mockEventAggregator.Verify(ea => ea.Subscribe(It.IsAny<Action<TextChangedEventArgs>>()),
            Times.Once);
        _mockEventAggregator.Verify(
            ea => ea.Subscribe(It.IsAny<Action<CursorPositionChangedEventArgs>>()),
            Times.Once);
        _mockEventAggregator.Verify(
            ea => ea.Subscribe(It.IsAny<Action<SelectionChangedEventArgs>>()), Times.Once);
        _mockEventAggregator.Verify(
            ea => ea.Subscribe(It.IsAny<Action<IsSelectingChangedEventArgs>>()),
            Times.Once);
    }

    [Fact]
    public void UpdateViewProperties_UpdatesAllRelevantProperties()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(10);
        _mockTextBuffer.Setup(tb => tb.GetText(It.IsAny<int>(), It.IsAny<int>())).Returns("Sample text");
        _mockTextMeasurer.Setup(tm => tm.MeasureWidth(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<string>()))
            .Returns(100);
        _viewModel.LineHeight = 15;
        _viewModel.WindowWidth = 500;
        _viewModel.WindowHeight = 300;

        // Act
        _viewModel.UpdateViewProperties();

        // Assert
        Assert.Equal(300, _viewModel.TotalHeight);
        Assert.Equal(500, _viewModel.LongestLineWidth);
        Assert.Equal(500, _viewModel.RequiredWidth);
        Assert.Equal(300, _viewModel.RequiredHeight);
    }

    [Fact]
    public void InvalidateLongestLine_ResetsLongestLineLengthCache()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(3);
        _mockTextBuffer.Setup(tb => tb.GetLineLength(It.IsAny<int>())).Returns(10);

        // Act
        _viewModel.InvalidateLongestLine();
        var longestLineLength = _viewModel.LongestLineLength;

        // Assert
        Assert.Equal(10, longestLineLength);
        _mockTextBuffer.Verify(tb => tb.GetLineLength(It.IsAny<int>()), Times.Exactly(3));
    }

    [Fact]
    public void InsertNewLine_InsertsNewLineAndUpdatesCursor()
    {
        // Arrange
        var cursorPosition = 5;
        _mockCursorManager.Setup(cm => cm.Position).Returns(() => cursorPosition);
        _mockCursorManager.Setup(cm => cm.MoveCursorRight(false))
            .Callback(() => cursorPosition += Environment.NewLine.Length);
        _mockTextBuffer.Setup(tb => tb.GetLineStarts()).Returns(new List<int> { 0, 6 });
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(It.IsAny<int>())).Returns(1);
        _mockTextBuffer.Setup(tb => tb.GetLineLength(It.IsAny<int>())).Returns(0);

        TextChangedEventArgs? capturedTextChangedArgs = null;
        _mockEventAggregator.Setup(ea => ea.Publish(It.IsAny<TextChangedEventArgs>()))
            .Callback<TextChangedEventArgs>(args => capturedTextChangedArgs = args);

        // Act
        _viewModel.InsertNewLine();

        // Assert
        _mockTextBuffer.Verify(tb => tb.InsertText(5, Environment.NewLine), Times.Once);
        _mockCursorManager.Verify(cm => cm.MoveCursorRight(false), Times.Once);

        Assert.NotNull(capturedTextChangedArgs);
        Assert.Equal(5, capturedTextChangedArgs.Position);
        Assert.Equal(Environment.NewLine, capturedTextChangedArgs.InsertedText);
        Assert.Equal(0, capturedTextChangedArgs.DeletedLength);

        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<TextChangedEventArgs>()), Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<CursorPositionChangedEventArgs>()), Times.Once);

        Assert.Equal(5 + Environment.NewLine.Length, cursorPosition); // Verify the cursor position has been updated
    }
    
    [Fact]
    public void UpdateLongestLineWidth_UpdatesLongestLineWidthProperty()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(3);
        _mockTextBuffer.Setup(tb => tb.GetLineText(It.IsAny<int>())).Returns("Very very very long line");
        _mockTextMeasurer.Setup(tm => tm.MeasureWidth(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<string>()))
            .Returns((string text, double fontSize, string fontFamily) =>
                text.Length * 5);
        _viewModel.WindowWidth = 10;

        // Act
        _viewModel.UpdateLongestLineWidth();

        // Assert
        Assert.Equal(120, _viewModel.LongestLineWidth); // "Very very very long line" length (24) * 5
        _mockTextBuffer.Verify(tb => tb.GetLineText(It.IsAny<int>()), Times.Exactly(6));
        _mockTextMeasurer.Verify(tm => tm.MeasureWidth(It.IsAny<string>(), _viewModel.FontSize, _viewModel.FontFamily),
            Times.Exactly(6));
    }

    [Fact]
    public void FontFamily_SetNewValue_UpdatesPropertyAndLineHeight()
    {
        // Act
        _viewModel.FontFamily = "Arial";

        // Assert
        Assert.Equal("Arial", _viewModel.FontFamily);
        _mockTextMeasurer.Verify(tm => tm.GetLineHeight(It.IsAny<double>(), "Arial"), Times.Once);
    }

    [Fact]
    public void FontSize_SetNewValue_UpdatesPropertyAndLineHeight()
    {
        // Act
        _viewModel.FontSize = 16;

        // Assert
        Assert.Equal(16, _viewModel.FontSize);
        _mockTextMeasurer.Verify(tm => tm.GetLineHeight(16, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void CursorPosition_SetNewValue_UpdatesPropertyAndTriggersEvent()
    {
        // Arrange
        int? newPosition = null;
        _viewModel.CursorPositionChanged += (_, args) => newPosition = args.Position;

        _mockTextBuffer.Setup(tb => tb.GetLineStarts()).Returns(new List<int> { 0 });
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(5)).Returns(0);
        _mockTextBuffer.Setup(tb => tb.GetLineLength(0)).Returns(10);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);

        // Act
        _viewModel.CursorPosition = 5;

        // Assert
        Assert.Equal(5, _viewModel.CursorPosition);
        Assert.Equal(5, newPosition);
    }

    [Fact]
    public void InsertText_InsertsTextAndUpdatesCursor()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(5);

        // Act
        _viewModel.InsertText(0, "Hello");

        // Assert
        _mockTextBuffer.Verify(tb => tb.InsertText(0, "Hello"), Times.Once);
        Assert.Equal(5, _viewModel.CursorPosition);
        _mockSelectionHandler.Verify(sh => sh.ClearSelection(), Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<TextChangedEventArgs>(e =>
            e.Position == 0 && e.InsertedText == "Hello" && e.DeletedLength == 0)), Times.Once);
    }

    [Fact]
    public void DeleteText_DeletesTextAndUpdatesCursor()
    {
        // Act
        _viewModel.DeleteText(0, 5);

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(0, 5), Times.Once);
        Assert.Equal(0, _viewModel.CursorPosition);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<TextChangedEventArgs>(e =>
            e.Position == 0 && e.InsertedText == "" && e.DeletedLength == 5)), Times.Once);
    }

    [Fact]
    public void HandleBackspace_WithSelection_DeletesSelection()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);
        _mockSelectionHandler.Setup(sh => sh.SelectionStart).Returns(0);
        _mockSelectionHandler.Setup(sh => sh.SelectionEnd).Returns(5);

        // Act
        _viewModel.HandleBackspace();

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(0, 5), Times.Once);
        _mockCursorManager.Verify(cm => cm.SetPosition(0), Times.Once);
        _mockSelectionHandler.Verify(sh => sh.ClearSelection(), Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<TextChangedEventArgs>(e =>
            e.Position == 0 && e.InsertedText == "" && e.DeletedLength == 5)), Times.Once);
    }

    [Fact]
    public void HandleBackspace_WithoutSelection_DeletesPreviousCharacter()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(false);
        var cursorPosition = 5;
        _mockCursorManager.Setup(cm => cm.Position).Returns(() => cursorPosition);
        _mockCursorManager.Setup(cm => cm.MoveCursorLeft(false))
            .Callback(() => cursorPosition--);

        TextChangedEventArgs? capturedTextChangedArgs = null;
        _mockEventAggregator.Setup(ea => ea.Publish(It.IsAny<TextChangedEventArgs>()))
            .Callback<TextChangedEventArgs>(args => capturedTextChangedArgs = args);

        // Act
        _viewModel.HandleBackspace();

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(4, 1), Times.Once);
        _mockCursorManager.Verify(cm => cm.MoveCursorLeft(false), Times.Once);

        Assert.NotNull(capturedTextChangedArgs);
        Assert.Equal(4, capturedTextChangedArgs.Position);
        Assert.Equal("", capturedTextChangedArgs.InsertedText);
        Assert.Equal(1, capturedTextChangedArgs.DeletedLength);

        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<TextChangedEventArgs>()), Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<CursorPositionChangedEventArgs>()), Times.Once);

        Assert.Equal(4, cursorPosition); // Verify the cursor position has been updated
    }

    [Fact]
    public async Task CopyText_WithSelection_CopiesSelectedText()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);
        _mockSelectionHandler.Setup(sh => sh.SelectionStart).Returns(0);
        _mockSelectionHandler.Setup(sh => sh.SelectionEnd).Returns(5);
        _mockTextBuffer.Setup(tb => tb.GetText(0, 5)).Returns("Hello");

        // Act
        await _viewModel.CopyText();

        // Assert
        _mockClipboardService.Verify(cs => cs.SetTextAsync("Hello"), Times.Once);
    }

    [Fact]
    public async Task PasteText_InsertsClipboardText()
    {
        // Arrange
        _mockClipboardService.Setup(cs => cs.GetTextAsync()).ReturnsAsync("Pasted");
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);

        // Act
        await _viewModel.PasteText();

        // Assert
        _mockTextBuffer.Verify(tb => tb.InsertText(5, "Pasted"), Times.Once);
        _mockCursorManager.Verify(cm => cm.SetPosition(11), Times.Once);
    }

    [Fact]
    public void StartSelection_InitializesSelection()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);
        _viewModel.CursorPosition = 5;

        // Act
        _viewModel.StartSelection();

        // Assert
        Assert.True(_viewModel.IsSelecting);
        Assert.Equal(5, _viewModel.SelectionStart);
        Assert.Equal(5, _viewModel.SelectionEnd);

        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<IsSelectingChangedEventArgs>(e => e.IsSelecting == true)),
            Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<SelectionChangedEventArgs>(e =>
            e.NewStart == 5 && e.NewEnd == 5 && e.IsSelecting == true)), Times.Once);
    }

    [Fact]
    public void UpdateSelection_UpdatesSelectionEnd()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.Length).Returns(15);
        _viewModel.IsSelecting = true;
        _viewModel.SelectionStart = 0;
        _viewModel.CursorPosition = 10;

        // Act
        _viewModel.UpdateSelection();

        // Assert
        _mockSelectionHandler.Verify(sh => sh.UpdateSelection(10), Times.Once());
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<SelectionChangedEventArgs>(e =>
            e.NewStart == 0 && e.NewEnd == 10 && e.IsSelecting == true)), Times.Once());
    }

    [Fact]
    public void ClearSelection_ResetsSelectionProperties()
    {
        // Arrange
        _viewModel.IsSelecting = true;
        _viewModel.SelectionStart = 0;
        _viewModel.SelectionEnd = 10;

        // Act
        _viewModel.ClearSelection();

        // Assert
        Assert.False(_viewModel.IsSelecting);
        Assert.Equal(-1, _viewModel.SelectionStart);
        Assert.Equal(-1, _viewModel.SelectionEnd);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<IsSelectingChangedEventArgs>(e => e.IsSelecting == false)),
            Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<SelectionChangedEventArgs>(e =>
            e.NewStart == -1 && e.NewEnd == -1 && e.IsSelecting == false)), Times.Once);
    }

    [Fact]
    public void GetSelectedText_ReturnsCorrectText()
    {
        // Arrange
        _viewModel.IsSelecting = true;
        _viewModel.SelectionStart = 0;
        _viewModel.SelectionEnd = 5;
        _mockTextBuffer.Setup(tb => tb.GetText(0, 5)).Returns("Hello");

        // Act
        var selectedText = _viewModel.GetSelectedText();

        // Assert
        Assert.Equal("Hello", selectedText);
    }

    [Fact]
    public void HandleBackspace_AtBeginningOfText_DoesNothing()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(false);
        _mockCursorManager.Setup(cm => cm.Position).Returns(0);

        // Act
        _viewModel.HandleBackspace();

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockCursorManager.Verify(cm => cm.MoveCursorLeft(false), Times.Never);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<TextChangedEventArgs>()), Times.Never);
    }

    [Fact]
    public void HandleDelete_WithSelection_DeletesSelection()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(true);
        _mockSelectionHandler.Setup(sh => sh.SelectionStart).Returns(0);
        _mockSelectionHandler.Setup(sh => sh.SelectionEnd).Returns(5);

        // Act
        _viewModel.HandleDelete();

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(0, 5), Times.Once);
        _mockSelectionHandler.Verify(sh => sh.ClearSelection(), Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<TextChangedEventArgs>(e =>
            e.Position == 0 && e.InsertedText == "" && e.DeletedLength == 5)), Times.Once);
    }

    [Fact]
    public void HandleDelete_WithoutSelection_DeletesNextCharacter()
    {
        // Arrange
        _mockSelectionHandler.Setup(sh => sh.HasSelection).Returns(false);
        _mockCursorManager.Setup(cm => cm.Position).Returns(5);
        _mockTextBuffer.Setup(tb => tb.Length).Returns(10);

        // Act
        _viewModel.HandleDelete();

        // Assert
        _mockTextBuffer.Verify(tb => tb.DeleteText(5, 1), Times.Once);
        _mockCursorManager.Verify(cm => cm.SetPosition(5), Times.Once);
        _mockEventAggregator.Verify(ea => ea.Publish(It.Is<TextChangedEventArgs>(e =>
            e.Position == 5 && e.InsertedText == "" && e.DeletedLength == 1)), Times.Once);
    }

    [Fact]
    public void Offset_SetNewValue_UpdatesPropertyAndNotifiesChange()
    {
        // Arrange
        var newOffset = new Vector(10, 20);
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, args) => propertyChangedEvents.Add(args.PropertyName);

        // Act
        _viewModel.Offset = newOffset;

        // Assert
        Assert.Equal(newOffset, _viewModel.Offset);
        Assert.Contains("Offset", propertyChangedEvents);
        Assert.Contains("VerticalOffset", propertyChangedEvents);
        Assert.Contains("HorizontalOffset", propertyChangedEvents);
        Assert.Equal(20, _viewModel.VerticalOffset);
        Assert.Equal(10, _viewModel.HorizontalOffset);
        _mockLineCountViewModel.VerifySet(lcvm => lcvm.VerticalOffset = 20, Times.Once);
    }

    [Fact]
    public void VerticalOffset_SetNewValue_UpdatesOffsetAndNotifiesChange()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, args) => propertyChangedEvents.Add(args.PropertyName);

        // Act
        _viewModel.VerticalOffset = 30;

        // Assert
        Assert.Equal(30, _viewModel.VerticalOffset);
        Assert.Equal(30, _viewModel.Offset.Y);
        Assert.Contains("VerticalOffset", propertyChangedEvents);
        Assert.Contains("Offset", propertyChangedEvents);
        _mockLineCountViewModel.VerifySet(lcvm => lcvm.VerticalOffset = 30, Times.Once);
    }

    [Fact]
    public void HorizontalOffset_SetNewValue_UpdatesOffsetAndNotifiesChange()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (sender, args) => propertyChangedEvents.Add(args.PropertyName);

        // Act
        _viewModel.HorizontalOffset = 40;

        // Assert
        Assert.Equal(40, _viewModel.HorizontalOffset);
        Assert.Equal(40, _viewModel.Offset.X);
        Assert.Contains("HorizontalOffset", propertyChangedEvents);
        Assert.Contains("Offset", propertyChangedEvents);
    }

    [Fact]
    public void WindowWidth_SetNewValue_UpdatesPropertyAndLongestLineWidth()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.GetText(It.IsAny<int>(), It.IsAny<int>()))
            .Returns("Sample text\nLonger sample text");
        _mockTextMeasurer.Setup(tm => tm.MeasureWidth(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<string>()))
            .Returns(50);

        // Act
        _viewModel.WindowWidth = 100;

        // Assert
        Assert.Equal(100, _viewModel.WindowWidth);
        Assert.Equal(100, _viewModel.LongestLineWidth);
        Assert.Equal(100, _viewModel.RequiredWidth);
    }

    [Fact]
    public void WindowHeight_SetNewValue_UpdatesPropertyAndTotalHeight()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(5);
        _viewModel.LineHeight = 10;

        // Act
        _viewModel.WindowHeight = 100;

        // Assert
        Assert.Equal(100, _viewModel.WindowHeight);
        Assert.Equal(100, _viewModel.TotalHeight);
        Assert.Equal(100, _viewModel.RequiredHeight);
    }

    [Fact]
    public void ViewportWidth_SetNewValue_UpdatesPropertyAndRequiredWidth()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(2);
        _mockTextBuffer.Setup(tb => tb.GetLineText(It.IsAny<int>()))
            .Returns((int i) => i == 0 ? "Sample text" : "Longer sample text");
        _mockTextMeasurer.Setup(tm => tm.MeasureWidth(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<string>()))
            .Returns(50);
        _viewModel.WindowWidth = 100; // Set initial window width

        // Act
        _viewModel.ViewportWidth = 120;

        // Assert
        Assert.Equal(120, _viewModel.ViewportWidth);
        Assert.Equal(120, _viewModel.RequiredWidth);

        // Force update of view properties
        _viewModel.UpdateViewProperties();

        // Verify that MeasureWidth was called at least once with any parameters
        _mockTextMeasurer.Verify(
            tm => tm.MeasureWidth(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<string>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public void ViewportHeight_SetNewValue_UpdatesPropertyAndTotalHeight()
    {
        // Arrange
        _mockTextBuffer.Setup(tb => tb.LineCount).Returns(5);
        _viewModel.LineHeight = 10;

        // Act
        _viewModel.ViewportHeight = 120;

        // Assert
        Assert.Equal(120, _viewModel.ViewportHeight);
        Assert.Equal(120, _viewModel.RequiredHeight);
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Act
        _viewModel.Dispose();

        // Assert
        _mockEventAggregator.Verify(
            ea => ea.Unsubscribe(It.IsAny<Action<TextChangedEventArgs>>()), Times.Once);
        _mockEventAggregator.Verify(
            ea => ea.Unsubscribe(It.IsAny<Action<CursorPositionChangedEventArgs>>()),
            Times.Once);
        _mockEventAggregator.Verify(
            ea => ea.Unsubscribe(It.IsAny<Action<SelectionChangedEventArgs>>()), Times.Once);
        _mockEventAggregator.Verify(
            ea => ea.Unsubscribe(It.IsAny<Action<IsSelectingChangedEventArgs>>()),
            Times.Once);
    }
}