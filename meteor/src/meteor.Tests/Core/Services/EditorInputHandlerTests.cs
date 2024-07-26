using System.Diagnostics;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.EventArgs;
using meteor.UI.Services;
using Moq;

namespace meteor.Tests.Core.Services;

public class EditorInputHandlerTests
{
    private readonly Mock<IEditorViewModel> _mockEditorViewModel;

    public EditorInputHandlerTests()
    {
        _mockEditorViewModel = new Mock<IEditorViewModel>();
        Mock<IScrollManager> mockScrollManager = new();
        new EditorInputHandler(_mockEditorViewModel.Object, mockScrollManager.Object);
    }

    [Fact]
    public void InsertText_LargeDocumentPerformance_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var largeText = new string('a', 10000000); // 10,000,000 characters
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        _mockEditorViewModel.Object.HandleTextInput(new TextInputEventArgs(largeText));
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 125,
            $"Inserting text took {stopwatch.ElapsedMilliseconds}ms, which exceeds the 125ms limit");
    }
}