using meteor.Interfaces;
using meteor.Models;
using meteor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using File = System.IO.File;

namespace tests.ViewModels;

public class TabViewModelTests : IDisposable
{
    private readonly Mock<ICursorPositionService> _mockCursorPositionService;
    private readonly Mock<IUndoRedoManager<TextState>> _mockUndoRedoManager;
    private readonly Mock<IFileSystemWatcherFactory> _mockFileSystemWatcherFactory;
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly FontPropertiesViewModel _fontPropertiesViewModel;
    private readonly LineCountViewModel _lineCountViewModel;
    private readonly Mock<IClipboardService> _mockClipboardService;

    public TabViewModelTests()
    {
        _mockCursorPositionService = new Mock<ICursorPositionService>();
        _mockUndoRedoManager = new Mock<IUndoRedoManager<TextState>>();
        _mockFileSystemWatcherFactory = new Mock<IFileSystemWatcherFactory>();
        _mockTextBuffer = new Mock<ITextBuffer>();
        _fontPropertiesViewModel = new FontPropertiesViewModel();
        _lineCountViewModel = new LineCountViewModel();
        _mockClipboardService = new Mock<IClipboardService>();

        _mockFileSystemWatcherFactory
            .Setup(f => f.Create(It.IsAny<string>()))
            .Returns(() => { return new FileSystemWatcher(); });
        
        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();
        meteor.App.SetServiceProviderForTesting(serviceProvider);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_mockCursorPositionService.Object);
        services.AddSingleton(_mockUndoRedoManager.Object);
        services.AddSingleton(_mockFileSystemWatcherFactory.Object);
        services.AddSingleton(_mockTextBuffer.Object);
        services.AddSingleton(_fontPropertiesViewModel);
        services.AddSingleton(_lineCountViewModel);
        services.AddSingleton(_mockClipboardService.Object);
    }

    public void Dispose()
    {
        meteor.App.SetServiceProviderForTesting(null);
    }

    private TabViewModel CreateTabViewModel()
    {
        return new TabViewModel(
            _mockCursorPositionService.Object,
            _mockUndoRedoManager.Object,
            _mockFileSystemWatcherFactory.Object,
            _mockTextBuffer.Object,
            _fontPropertiesViewModel,
            _lineCountViewModel,
            _mockClipboardService.Object);
    }


    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var tabViewModel = CreateTabViewModel();

        // Assert
        Assert.Equal("Untitled", tabViewModel.Title);
        Assert.False(tabViewModel.IsNew);
        Assert.False(tabViewModel.IsSelected);
        Assert.False(tabViewModel.IsTemporary);
        Assert.False(tabViewModel.IsDirty);
        Assert.Empty(tabViewModel.Text);
        Assert.Empty(tabViewModel.OriginalText);
        Assert.Null(tabViewModel.FilePath);
        Assert.NotNull(tabViewModel.ScrollableTextEditorViewModel);
    }

    [Fact]
    public void Text_SetsDirtyFlag_WhenChanged()
    {
        // Arrange
        var tabViewModel = CreateTabViewModel();
        tabViewModel.OriginalText = "Original text";

        // Act
        tabViewModel.Text = "New text";

        // Assert
        Assert.True(tabViewModel.IsDirty);
    }

    [Fact]
    public async Task LoadTextAsync_LoadsTextAndUpdatesProperties()
    {
        // Arrange
        var tabViewModel = CreateTabViewModel();
        var testFilePath = Path.GetTempFileName();
        var testContent = "Test content";
        File.WriteAllText(testFilePath, testContent);

        try
        {
            // Act
            await tabViewModel.LoadTextAsync(testFilePath);

            // Assert
            Assert.Equal(testContent, tabViewModel.Text);
            Assert.Equal(testContent, tabViewModel.OriginalText);
            Assert.Equal(testFilePath, tabViewModel.FilePath);
            Assert.False(tabViewModel.IsNew);
            Assert.False(tabViewModel.IsDirty);
        }
        finally
        {
            // Clean up
            File.Delete(testFilePath);
        }
    }

    [Fact]
    public void Undo_RestoresPreviousState()
    {
        // Arrange
        var initialState = new TextState("Initial text", 0);
        var modifiedState = new TextState("Modified text", 0);

        _mockUndoRedoManager.Setup(m => m.CanUndo).Returns(true);
        _mockUndoRedoManager.Setup(m => m.Undo())
            .Returns((initialState, "Undo description"));

        var tabViewModel = CreateTabViewModel();
        tabViewModel.Text = "Modified text";

        // Act
        tabViewModel.Undo();

        // Assert
        Assert.Equal("Initial text", tabViewModel.Text);
        _mockUndoRedoManager.Verify(m => m.Undo(), Times.Once);
    }

    [Fact]
    public void Redo_RestoresUndoneState()
    {
        // Arrange
        var tabViewModel = CreateTabViewModel();
        tabViewModel.Text = "Initial text";
        tabViewModel.OriginalText = "Initial text";
        tabViewModel.Text = "Modified text";
        tabViewModel.Undo();

        // Act
        tabViewModel.Redo();

        // Assert
        Assert.Equal("Modified text", tabViewModel.Text);
    }

    [Fact]
    public async Task SaveAsync_UpdatesOriginalTextAndClearsDirtyFlag()
    {
        // Arrange
        var tabViewModel = CreateTabViewModel();
        var testFilePath = Path.GetTempFileName();
        tabViewModel.FilePath = testFilePath;
        tabViewModel.Text = "New content";
        tabViewModel.OriginalText = "Old content";
        tabViewModel.IsDirty = true; // Ensure IsDirty is set to true

        try
        {
            // Act
            await tabViewModel.SaveAsync();

            // Assert
            Assert.Equal("New content", tabViewModel.OriginalText);
            Assert.False(tabViewModel.IsDirty);
            Assert.Equal("New content", await File.ReadAllTextAsync(testFilePath));
        }
        finally
        {
            // Clean up
            if (File.Exists(testFilePath)) File.Delete(testFilePath);
        }
    }
}