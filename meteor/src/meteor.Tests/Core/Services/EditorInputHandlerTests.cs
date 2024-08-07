using System.Diagnostics;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.EventArgs;
using Moq;

namespace meteor.Tests.Core.Services;

public class EditorInputHandlerTests
{
    private readonly Mock<IEditorViewModel> _mockEditorViewModel;

    public EditorInputHandlerTests()
    {
        _mockEditorViewModel = new Mock<IEditorViewModel>();
    }

    [Fact]
    public void InsertText_LargeDocumentPerformance_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var largeText = new string('a', 10_000_000);
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