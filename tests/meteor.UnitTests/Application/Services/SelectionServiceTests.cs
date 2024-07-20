using meteor.Core.Services;

namespace meteor.UnitTests.Application.Services;

public class SelectionServiceTests
{
    private readonly SelectionService _selectionService;

    public SelectionServiceTests()
    {
        _selectionService = new SelectionService();
    }

    [Fact]
    public void StartSelection_SetsSelectionStartAndClearsLength()
    {
        _selectionService.StartSelection(5);
        var (start, length) = _selectionService.GetSelection();
        Assert.Equal(5, start);
        Assert.Equal(0, length);
    }

    [Fact]
    public void UpdateSelection_CalculatesCorrectLength()
    {
        _selectionService.StartSelection(5);
        _selectionService.UpdateSelection(10);
        var (start, length) = _selectionService.GetSelection();
        Assert.Equal(5, start);
        Assert.Equal(5, length);
    }

    [Fact]
    public void UpdateSelection_CalculatesNegativeLengthCorrectly()
    {
        _selectionService.StartSelection(10);
        _selectionService.UpdateSelection(5);
        var (start, length) = _selectionService.GetSelection();
        Assert.Equal(5, start);
        Assert.Equal(5, length);
    }

    [Fact]
    public void ClearSelection_ResetsStartAndLength()
    {
        _selectionService.StartSelection(5);
        _selectionService.UpdateSelection(10);
        _selectionService.ClearSelection();
        var (start, length) = _selectionService.GetSelection();
        Assert.Equal(-1, start);
        Assert.Equal(0, length);
    }

    [Fact]
    public void GetSelection_ReturnsCorrectValues()
    {
        _selectionService.StartSelection(5);
        _selectionService.UpdateSelection(8);
        var (start, length) = _selectionService.GetSelection();
        Assert.Equal(5, start);
        Assert.Equal(3, length);
    }

    [Fact]
    public void MultipleSelections_WorkCorrectly()
    {
        _selectionService.StartSelection(5);
        _selectionService.UpdateSelection(10);
        var (start1, length1) = _selectionService.GetSelection();
        Assert.Equal(5, start1);
        Assert.Equal(5, length1);

        _selectionService.StartSelection(15);
        _selectionService.UpdateSelection(12);
        var (start2, length2) = _selectionService.GetSelection();
        Assert.Equal(12, start2);
        Assert.Equal(3, length2);
    }
}