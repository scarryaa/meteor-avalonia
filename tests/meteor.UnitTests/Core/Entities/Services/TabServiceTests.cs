using meteor.Core.Interfaces.Services;
using meteor.Core.Services;
using Moq;

namespace meteor.UnitTests.Core.Entities.Services;

public class TabServiceTests
{
    private readonly TabService _tabService;
    private readonly Mock<ITextBufferService> _mockTextBufferService;

    public TabServiceTests()
    {
        _tabService = new TabService();
        _mockTextBufferService = new Mock<ITextBufferService>();
    }

    [Fact]
    public void GetActiveTextBufferService_NoActiveTabs_ReturnsNewTextBufferService()
    {
        var result = _tabService.GetActiveTextBufferService();

        Assert.NotNull(result);
        Assert.IsType<TextBufferService>(result);
    }

    [Fact]
    public void GetActiveTextBufferService_WithActiveTab_ReturnsCorrectTextBufferService()
    {
        var tabInfo = _tabService.AddTab(_mockTextBufferService.Object);
        _tabService.SwitchTab(tabInfo.Index);

        var result = _tabService.GetActiveTextBufferService();

        Assert.Same(_mockTextBufferService.Object, result);
    }

    [Fact]
    public void SwitchTab_ValidIndex_SwitchesActiveTab()
    {
        var tabInfo1 = _tabService.AddTab(_mockTextBufferService.Object);
        var tabInfo2 = _tabService.AddTab(new Mock<ITextBufferService>().Object);

        _tabService.SwitchTab(tabInfo2.Index);

        Assert.Equal(tabInfo2.Index, _tabService.GetActiveTab().Index);
    }

    [Fact]
    public void SwitchTab_InvalidIndex_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _tabService.SwitchTab(999));
    }

    [Fact]
    public void AddTab_CreatesNewTabWithCorrectIndex()
    {
        var tabInfo1 = _tabService.AddTab(_mockTextBufferService.Object);
        var tabInfo2 = _tabService.AddTab(new Mock<ITextBufferService>().Object);

        Assert.Equal(1, tabInfo1.Index);
        Assert.Equal(2, tabInfo2.Index);
    }

    [Fact]
    public void AddTab_SetsFirstTabAsActive()
    {
        var tabInfo = _tabService.AddTab(_mockTextBufferService.Object);

        Assert.Equal(tabInfo.Index, _tabService.GetActiveTab().Index);
    }

    [Fact]
    public void CloseTab_RemovesTabAndUpdatesActiveTab()
    {
        var tabInfo1 = _tabService.AddTab(_mockTextBufferService.Object);
        var tabInfo2 = _tabService.AddTab(new Mock<ITextBufferService>().Object);
        _tabService.SwitchTab(tabInfo2.Index);

        _tabService.CloseTab(tabInfo2.Index);

        Assert.Single(_tabService.GetAllTabs());
        Assert.Equal(tabInfo1.Index, _tabService.GetActiveTab().Index);
    }

    [Fact]
    public void CloseTab_LastTab_SetsActiveTabToNull()
    {
        var tabInfo = _tabService.AddTab(_mockTextBufferService.Object);

        _tabService.CloseTab(tabInfo.Index);

        Assert.Empty(_tabService.GetAllTabs());
        Assert.Null(_tabService.GetActiveTab());
    }

    [Fact]
    public void CloseAllTabs_RemovesAllTabsAndResetsState()
    {
        _tabService.AddTab(_mockTextBufferService.Object);
        _tabService.AddTab(new Mock<ITextBufferService>().Object);

        _tabService.CloseAllTabs();

        Assert.Empty(_tabService.GetAllTabs());
        Assert.Null(_tabService.GetActiveTab());
        // Verify that the next tab index is reset
        var newTab = _tabService.AddTab(_mockTextBufferService.Object);
        Assert.Equal(1, newTab.Index);
    }

    [Fact]
    public void CloseOtherTabs_KeepsSpecifiedTabAndClosesOthers()
    {
        var tabInfo1 = _tabService.AddTab(_mockTextBufferService.Object);
        var tabInfo2 = _tabService.AddTab(new Mock<ITextBufferService>().Object);
        var tabInfo3 = _tabService.AddTab(new Mock<ITextBufferService>().Object);

        _tabService.CloseOtherTabs(tabInfo2.Index);

        var remainingTabs = _tabService.GetAllTabs().ToList();
        Assert.Single(remainingTabs);
        Assert.Equal(tabInfo2.Index, remainingTabs[0].Index);
        Assert.Equal(tabInfo2.Index, _tabService.GetActiveTab().Index);
    }

    [Fact]
    public void CloseOtherTabs_InvalidIndex_DoesNothing()
    {
        _tabService.AddTab(_mockTextBufferService.Object);
        _tabService.AddTab(new Mock<ITextBufferService>().Object);

        _tabService.CloseOtherTabs(999);

        Assert.Equal(2, _tabService.GetAllTabs().Count());
    }

    [Fact]
    public void GetAllTabs_ReturnsAllTabs()
    {
        _tabService.AddTab(_mockTextBufferService.Object);
        _tabService.AddTab(new Mock<ITextBufferService>().Object);

        var allTabs = _tabService.GetAllTabs().ToList();

        Assert.Equal(2, allTabs.Count);
        Assert.Equal(1, allTabs[0].Index);
        Assert.Equal(2, allTabs[1].Index);
    }

    [Fact]
    public void GetActiveTab_NoActiveTabs_ReturnsNull()
    {
        Assert.Null(_tabService.GetActiveTab());
    }

    [Fact]
    public void GetActiveTab_WithActiveTabs_ReturnsCorrectTab()
    {
        var tabInfo1 = _tabService.AddTab(_mockTextBufferService.Object);
        var tabInfo2 = _tabService.AddTab(new Mock<ITextBufferService>().Object);
        _tabService.SwitchTab(tabInfo2.Index);

        var activeTab = _tabService.GetActiveTab();

        Assert.Equal(tabInfo2.Index, activeTab.Index);
    }
}