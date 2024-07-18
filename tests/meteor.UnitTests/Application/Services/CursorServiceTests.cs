using meteor.Application.Services;
using meteor.Core.Interfaces.Services;
using Moq;

namespace meteor.UnitTests.Application.Services;

public class CursorServiceTests
{
    private readonly Mock<ITextBufferService> _textBufferServiceMock;

    public CursorServiceTests()
    {
        _textBufferServiceMock = new Mock<ITextBufferService>();
    }

    [Fact]
    public void GetCursorPosition_ReturnsInitialPosition()
    {
        // Arrange
        var cursorService = new CursorService(_textBufferServiceMock.Object);

        // Act
        var position = cursorService.GetCursorPosition();

        // Assert
        Assert.Equal(0, position);
    }

    [Fact]
    public void SetCursorPosition_UpdatesCursorPosition()
    {
        // Arrange
        var cursorService = new CursorService(_textBufferServiceMock.Object);
        var newPosition = 10;
        _textBufferServiceMock.Setup(tbs => tbs.Length).Returns(20);

        // Act
        cursorService.SetCursorPosition(newPosition);
        var position = cursorService.GetCursorPosition();

        // Assert
        Assert.Equal(newPosition, position);
    }

    [Fact]
    public void SetCursorPosition_DoesNotSetNegativePosition()
    {
        // Arrange
        var cursorService = new CursorService(_textBufferServiceMock.Object);
        var newPosition = -10;

        // Act
        cursorService.SetCursorPosition(newPosition);
        var position = cursorService.GetCursorPosition();

        // Assert
        Assert.Equal(0, position);
    }

    [Fact]
    public void MoveCursor_UpdatesCursorPositionBasedOnCoordinates()
    {
        // Arrange
        var text = "Line1\nLine2\nLine3\nLine4";
        _textBufferServiceMock.Setup(tbs => tbs.Length).Returns(text.Length);
        _textBufferServiceMock.Setup(tbs => tbs[It.IsAny<int>()]).Returns((int i) => text[i]);
        var cursorService = new CursorService(_textBufferServiceMock.Object);
        var x = 3;
        var y = 2;
        var expectedPosition = 15;

        // Act
        cursorService.MoveCursor(x, y);
        var position = cursorService.GetCursorPosition();

        // Assert
        Assert.Equal(expectedPosition, position);
    }

    [Fact]
    public void MoveCursor_DoesNotSetNegativePosition()
    {
        // Arrange
        var text = "Line1\n";
        _textBufferServiceMock.Setup(tbs => tbs.Length).Returns(text.Length);
        _textBufferServiceMock.Setup(tbs => tbs[It.IsAny<int>()]).Returns((int i) => text[i]);
        var cursorService = new CursorService(_textBufferServiceMock.Object);
        var x = -5;
        var y = -2;

        // Act
        cursorService.MoveCursor(x, y);
        var position = cursorService.GetCursorPosition();

        // Assert
        Assert.Equal(0, position);
    }
}