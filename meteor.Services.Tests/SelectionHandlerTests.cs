using meteor.Core.Interfaces;
using meteor.Core.Interfaces.Events;
using meteor.Core.Models.Events;
using Moq;

namespace meteor.Services.Tests;

public class SelectionHandlerTests
{
    private readonly Mock<ITextBuffer> _mockTextBuffer;
    private readonly Mock<IWordBoundaryService> _mockWordBoundaryService;
    private readonly Mock<IEventAggregator> _mockEventAggregator;
    private readonly SelectionHandler _selectionHandler;

    public SelectionHandlerTests()
    {
        _mockTextBuffer = new Mock<ITextBuffer>();
        _mockWordBoundaryService = new Mock<IWordBoundaryService>();
        _mockEventAggregator = new Mock<IEventAggregator>();
        _selectionHandler = new SelectionHandler(_mockTextBuffer.Object, _mockWordBoundaryService.Object,
            _mockEventAggregator.Object);

        _mockEventAggregator.Invocations.Clear();
    }

    [Fact]
    public void StartSelection_ShouldSetSelectionAndPublishEvent()
    {
        _selectionHandler.StartSelection(5);
        Assert.True(_selectionHandler.IsSelecting);
        Assert.Equal(5, _selectionHandler.SelectionAnchor);
        Assert.Equal(5, _selectionHandler.SelectionStart);
        Assert.Equal(5, _selectionHandler.SelectionEnd);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<SelectionChangedEventArgs>()), Times.Once);
    }

    [Fact]
    public void UpdateSelection_ShouldUpdateSelectionEndAndPublishEvent()
    {
        _selectionHandler.StartSelection(5);
        _selectionHandler.UpdateSelection(10);
        Assert.Equal(5, _selectionHandler.SelectionStart);
        Assert.Equal(10, _selectionHandler.SelectionEnd);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<SelectionChangedEventArgs>()), Times.Exactly(2));
    }

    [Fact]
    public void UpdateSelectionDuringDrag_TripleClick_ShouldSelectWholeLine()
    {
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(It.IsAny<int>())).Returns(1);
        _mockTextBuffer.Setup(tb => tb.GetLineEndPosition(1)).Returns(20);

        _selectionHandler.StartSelection(5);
        _selectionHandler.UpdateSelectionDuringDrag(10, false, true);

        Assert.Equal(5, _selectionHandler.SelectionStart);
        Assert.Equal(20, _selectionHandler.SelectionEnd);
    }

    [Fact]
    public void UpdateSelectionDuringDrag_DoubleClick_ShouldSelectWord()
    {
        _mockWordBoundaryService.Setup(wbs => wbs.GetWordBoundaries(It.IsAny<ITextBuffer>(), It.IsAny<int>()))
            .Returns((3, 8));

        _selectionHandler.StartSelection(5);
        _selectionHandler.UpdateSelectionDuringDrag(10, true, false);

        Assert.Equal(5, _selectionHandler.SelectionStart);
        Assert.Equal(8, _selectionHandler.SelectionEnd);
    }

    [Fact]
    public void ClearSelection_ShouldResetSelectionAndPublishEvent()
    {
        _selectionHandler.StartSelection(5);
        _mockEventAggregator.Invocations.Clear();
        _selectionHandler.ClearSelection();
        Assert.False(_selectionHandler.HasSelection);
        Assert.False(_selectionHandler.IsSelecting);
        Assert.Equal(-1, _selectionHandler.SelectionStart);
        Assert.Equal(-1, _selectionHandler.SelectionEnd);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<SelectionChangedEventArgs>()), Times.Once);
    }

    [Fact]
    public void SelectAll_ShouldSelectEntireTextAndPublishEvent()
    {
        _mockTextBuffer.Setup(tb => tb.Length).Returns(100);

        _selectionHandler.SelectAll();

        Assert.Equal(0, _selectionHandler.SelectionStart);
        Assert.Equal(100, _selectionHandler.SelectionEnd);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<SelectionChangedEventArgs>()), Times.Once);
    }

    [Fact]
    public void SelectWord_ShouldSelectWordBoundariesAndPublishEvent()
    {
        _mockWordBoundaryService.Setup(wbs => wbs.GetWordBoundaries(It.IsAny<ITextBuffer>(), It.IsAny<int>()))
            .Returns((5, 10));

        _selectionHandler.SelectWord(7);

        Assert.Equal(5, _selectionHandler.SelectionStart);
        Assert.Equal(10, _selectionHandler.SelectionEnd);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<SelectionChangedEventArgs>()), Times.Once);
    }

    [Fact]
    public void SelectLine_ShouldSelectEntireLineAndPublishEvent()
    {
        _mockTextBuffer.Setup(tb => tb.GetLineIndexFromPosition(It.IsAny<int>())).Returns(1);
        _mockTextBuffer.Setup(tb => tb.GetLineStartPosition(1)).Returns(10);
        _mockTextBuffer.Setup(tb => tb.GetLineEndPosition(1)).Returns(20);

        _selectionHandler.SelectLine(15);

        Assert.Equal(10, _selectionHandler.SelectionStart);
        Assert.Equal(20, _selectionHandler.SelectionEnd);
        _mockEventAggregator.Verify(ea => ea.Publish(It.IsAny<SelectionChangedEventArgs>()), Times.Once);
    }
}