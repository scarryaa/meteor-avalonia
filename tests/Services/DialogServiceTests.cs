using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using meteor.Enums;
using meteor.Interfaces;
using meteor.ViewModels;
using Moq;

namespace tests.Services;

public class DialogServiceTests
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly Mock<IMainWindowProvider> _mockMainWindowProvider;

    public DialogServiceTests()
    {
        // Mocking dependencies
        var titleBarViewModel = new Mock<TitleBarViewModel>(MockBehavior.Loose);
        var fontPropertiesViewModel = new Mock<FontPropertiesViewModel>(MockBehavior.Loose);
        var lineCountViewModel = new Mock<LineCountViewModel>(MockBehavior.Loose);
        var cursorPositionService = new Mock<ICursorPositionService>(MockBehavior.Loose);
        var statusPaneViewModel = new StatusPaneViewModel(cursorPositionService.Object);
        var fileExplorerViewModel = new Mock<FileExplorerViewModel>(MockBehavior.Loose);
        var textBufferFactory = new Mock<ITextBufferFactory>(MockBehavior.Loose);
        var autoSaveService = new Mock<IAutoSaveService>(MockBehavior.Loose);
        var dialogService = new Mock<IDialogService>(MockBehavior.Loose);

        // Creating MainWindowViewModel instance
        _mainWindowViewModel = new MainWindowViewModel(
            titleBarViewModel.Object,
            statusPaneViewModel,
            fontPropertiesViewModel.Object,
            lineCountViewModel.Object,
            cursorPositionService.Object,
            fileExplorerViewModel.Object,
            textBufferFactory.Object,
            autoSaveService.Object,
            dialogService.Object
        );

        _mockMainWindowProvider = new Mock<IMainWindowProvider>();
        _mockMainWindowProvider.Setup(p => p.GetMainWindow()).Returns((Window)null);
    }

    [AvaloniaFact]
    public async Task ShowContentDialogAsync_ShouldReturnPrimary_WhenPrimaryKeyPressed()
    {
        // Arrange
        var service = new TestDialogService(_mockMainWindowProvider.Object)
            { SimulatedResult = ContentDialogResult.Primary };
        var expectedTitle = "Test Title";
        var expectedContent = "Test Content";
        var expectedPrimaryButtonText = "OK";

        // Act
        var result = await service.ShowContentDialogAsync(_mainWindowViewModel, expectedTitle, expectedContent,
            expectedPrimaryButtonText);

        // Assert
        Assert.Equal(ContentDialogResult.Primary, result);
    }

    [AvaloniaFact]
    public async Task ShowContentDialogAsync_ShouldReturnSecondary_WhenSecondaryKeyPressed()
    {
        // Arrange
        var service = new TestDialogService(_mockMainWindowProvider.Object)
            { SimulatedResult = ContentDialogResult.Secondary };
        var expectedTitle = "Test Title";
        var expectedContent = "Test Content";
        var expectedPrimaryButtonText = "OK";
        var expectedSecondaryButtonText = "Cancel";

        // Act
        var result = await service.ShowContentDialogAsync(_mainWindowViewModel, expectedTitle, expectedContent,
            expectedPrimaryButtonText, expectedSecondaryButtonText);

        // Assert
        Assert.Equal(ContentDialogResult.Secondary, result);
    }

    [AvaloniaFact]
    public async Task ShowContentDialogAsync_ShouldReturnNone_WhenOtherKeyPressed()
    {
        // Arrange
        var service = new TestDialogService(_mockMainWindowProvider.Object)
            { SimulatedResult = ContentDialogResult.None };
        var expectedTitle = "Test Title";
        var expectedContent = "Test Content";
        var expectedPrimaryButtonText = "OK";

        // Act
        var result = await service.ShowContentDialogAsync(_mainWindowViewModel, expectedTitle, expectedContent,
            expectedPrimaryButtonText);

        // Assert
        Assert.Equal(ContentDialogResult.None, result);
    }

    [AvaloniaFact]
    public async Task ShowErrorDialogAsync_ShouldShowErrorDialog()
    {
        // Arrange
        var service = new TestDialogService(_mockMainWindowProvider.Object)
            { SimulatedResult = ContentDialogResult.Primary };
        var expectedMessage = "Error message";

        // Act
        var task = service.ShowErrorDialogAsync(_mainWindowViewModel, expectedMessage);

        // Wait for the dialog to be opened
        var opened = await service.DialogOpenedTcs.Task;

        // Assert
        Assert.True(opened, "IsDialogOpen should be true immediately after calling ShowErrorDialogAsync");

        await task;

        // Assert
        Assert.False(_mainWindowViewModel.IsDialogOpen,
            "IsDialogOpen should be false after ShowErrorDialogAsync completes");
    }
}