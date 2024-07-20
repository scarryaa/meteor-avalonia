using System.Text;
using meteor.Core.Enums;
using meteor.Core.Interfaces.Services;
using meteor.Core.Models;
using meteor.Core.Models.Events;
using meteor.UI.ViewModels;
using Moq;

public class EditorViewModelTests
{
    private readonly Mock<ITextBufferService> _mockTextBufferService;
    private readonly Mock<ITabService> _mockTabService;
    private readonly Mock<ISyntaxHighlighter> _mockSyntaxHighlighter;
    private readonly Mock<ISelectionService> _mockSelectionService;
    private readonly Mock<IInputService> _mockInputService;
    private readonly Mock<ICursorService> _mockCursorService;
    private readonly Mock<IEditorSizeCalculator> _mockSizeCalculator;
    private readonly EditorViewModel _viewModel;

    public EditorViewModelTests()
    {
        _mockTextBufferService = new Mock<ITextBufferService>();
        _mockTabService = new Mock<ITabService>();
        _mockSyntaxHighlighter = new Mock<ISyntaxHighlighter>();
        _mockSelectionService = new Mock<ISelectionService>();
        _mockInputService = new Mock<IInputService>();
        _mockCursorService = new Mock<ICursorService>();
        _mockSizeCalculator = new Mock<IEditorSizeCalculator>();

        _mockTabService.Setup(ts => ts.GetActiveTextBufferService()).Returns(_mockTextBufferService.Object);

        _viewModel = new EditorViewModel(
            _mockTextBufferService.Object,
            _mockTabService.Object,
            _mockSyntaxHighlighter.Object,
            _mockSelectionService.Object,
            _mockInputService.Object,
            _mockCursorService.Object,
            _mockSizeCalculator.Object);
    }

    [Fact]
    public void Text_Get_ReturnsCachedText()
    {
        // Arrange
        const string expectedText = "Hello, World!";
        _mockTextBufferService.Setup(tb => tb.AppendTo(It.IsAny<StringBuilder>()))
            .Callback<StringBuilder>(sb => sb.Append(expectedText));

        // Act
        var actualText = _viewModel.Text;

        // Assert
        Assert.Equal(expectedText, actualText);
        _mockTextBufferService.Verify(tb => tb.AppendTo(It.IsAny<StringBuilder>()), Times.Once);
    }

    [Fact]
    public void Text_Set_UpdatesTextBufferServiceAndRaisesPropertyChanged()
    {
        // Arrange
        const string newText = "New Text";
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.Text)) propertyChangedRaised = true;
        };

        // Act
        _viewModel.Text = newText;

        // Assert
        _mockTextBufferService.Verify(tb => tb.ReplaceAll(newText), Times.Once);
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void UpdateScrollOffset_UpdatesScrollOffsetProperty()
    {
        // Arrange
        var newOffset = new Vector(100, 200);

        // Act
        _viewModel.UpdateScrollOffset(newOffset);

        // Assert
        Assert.Equal(newOffset, _viewModel.ScrollOffset);
    }

    [Fact]
    public void UpdateWindowSize_UpdatesEditorSize()
    {
        // Arrange
        const double newWidth = 800;
        const double newHeight = 600;

        // Act
        _viewModel.UpdateWindowSize(newWidth, newHeight);

        // Assert
        _mockSizeCalculator.Verify(sc => sc.UpdateWindowSize(newWidth, newHeight), Times.Once);
    }

    [Fact]
    public void DeleteText_UpdatesTextAndRaisesPropertyChanged()
    {
        // Arrange
        const int index = 0;
        const int length = 5;
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.Text)) propertyChangedRaised = true;
        };

        // Act
        _viewModel.DeleteText(index, length);

        // Assert
        _mockInputService.Verify(input => input.DeleteText(index, length), Times.Once);
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void OnPointerPressed_UpdatesSelectionAndCursorPosition()
    {
        // Arrange
        var e = new PointerPressedEventArgs();

        // Act
        _viewModel.OnPointerPressed(e);

        // Assert
        _mockInputService.Verify(input => input.HandlePointerPressed(e), Times.Once);
    }

    [Fact]
    public void OnPointerMoved_UpdatesSelectionAndCursorPosition()
    {
        // Arrange
        var e = new PointerEventArgs();

        // Act
        _viewModel.OnPointerMoved(e);

        // Assert
        _mockInputService.Verify(input => input.HandlePointerMoved(e), Times.Once);
    }

    [Fact]
    public void OnPointerReleased_UpdatesSelectionAndCursorPosition()
    {
        // Arrange
        var e = new PointerReleasedEventArgs();

        // Act
        _viewModel.OnPointerReleased(e);

        // Assert
        _mockInputService.Verify(input => input.HandlePointerReleased(e), Times.Once);
    }

    [Fact]
    public void OnTextInput_UpdatesTextAndRaisesPropertyChanged()
    {
        // Arrange
        var e = new TextInputEventArgs();
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.Text)) propertyChangedRaised = true;
        };

        // Act
        _viewModel.OnTextInput(e);

        // Assert
        _mockInputService.Verify(input => input.HandleTextInput(e), Times.Once);
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public async Task OnKeyDown_UpdatesTextAndRaisesPropertyChanged()
    {
        // Arrange
        var e = new KeyEventArgs(Key.A, KeyModifiers.None);
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.Text)) propertyChangedRaised = true;
        };

        // Act
        await _viewModel.OnKeyDown(e);

        // Assert
        _mockInputService.Verify(input => input.HandleKeyDown(e.Key, e.Modifiers), Times.Once);
        Assert.True(propertyChangedRaised);
    }
}
