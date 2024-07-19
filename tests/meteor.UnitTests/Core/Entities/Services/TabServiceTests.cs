using meteor.Core.Interfaces.Services;
using meteor.Core.Services;
using Moq;

namespace meteor.UnitTests.Core.Entities.Services;

public class TabServiceTests
{
    [Fact]
    public void GetActiveTextBufferService_NoActiveTab_ReturnsNewTextBufferService()
    {
        // Arrange
        var tabService = new TabService();

        // Act
        var result = tabService.GetActiveTextBufferService();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TextBufferService>(result);
    }

    [Fact]
    public void GetActiveTextBufferService_WithActiveTab_ReturnsCorrectTextBufferService()
    {
        // Arrange
        var tabService = new TabService();
        var textBufferServiceMock = new Mock<ITextBufferService>();
        var tabIndex = tabService.GetNextAvailableTabIndex();
        tabService.RegisterTab(tabIndex, textBufferServiceMock.Object);
        tabService.SwitchTab(tabIndex);

        // Act
        var result = tabService.GetActiveTextBufferService();

        // Assert
        Assert.Same(textBufferServiceMock.Object, result);
    }

    [Fact]
    public void SwitchTab_ValidTabIndex_ChangesActiveTab()
    {
        // Arrange
        var tabService = new TabService();
        var textBufferServiceMock1 = new Mock<ITextBufferService>();
        var textBufferServiceMock2 = new Mock<ITextBufferService>();
        var tabIndex1 = tabService.GetNextAvailableTabIndex();
        var tabIndex2 = tabService.GetNextAvailableTabIndex();
        tabService.RegisterTab(tabIndex1, textBufferServiceMock1.Object);
        tabService.RegisterTab(tabIndex2, textBufferServiceMock2.Object);

        // Act
        tabService.SwitchTab(tabIndex2);

        // Assert
        var activeTextBufferService = tabService.GetActiveTextBufferService();
        Assert.Same(textBufferServiceMock2.Object, activeTextBufferService);
    }

    [Fact]
    public void SwitchTab_InvalidTabIndex_ThrowsArgumentException()
    {
        // Arrange
        var tabService = new TabService();
        var invalidTabIndex = 999;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tabService.SwitchTab(invalidTabIndex));
    }

    [Fact]
    public void RegisterTab_ValidParameters_AddsOrUpdatesTab()
    {
        // Arrange
        var tabService = new TabService();
        var textBufferServiceMock = new Mock<ITextBufferService>();
        var tabIndex = tabService.GetNextAvailableTabIndex();

        // Act
        tabService.RegisterTab(tabIndex, textBufferServiceMock.Object);

        // Assert
        var result = tabService.GetActiveTextBufferService();
        Assert.Same(textBufferServiceMock.Object, result);
    }

    [Fact]
    public void RegisterTab_NullTextBufferService_RemovesTab()
    {
        // Arrange
        var tabService = new TabService();
        var textBufferServiceMock = new Mock<ITextBufferService>();
        var tabIndex = tabService.GetNextAvailableTabIndex();
        tabService.RegisterTab(tabIndex, textBufferServiceMock.Object);
        tabService.RegisterTab(tabIndex, null);

        // Act
        var result = tabService.GetActiveTextBufferService();

        // Assert
        Assert.IsType<TextBufferService>(result);
    }

    [Fact]
    public void GetNextAvailableTabIndex_ReturnsUniqueIndex()
    {
        // Arrange
        var tabService = new TabService();

        // Act
        var index1 = tabService.GetNextAvailableTabIndex();
        var index2 = tabService.GetNextAvailableTabIndex();

        // Assert
        Assert.NotEqual(index1, index2);
        Assert.Equal(index1 + 1, index2);
    }
}