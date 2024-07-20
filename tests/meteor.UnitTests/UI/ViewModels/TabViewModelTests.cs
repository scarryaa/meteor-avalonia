using System.Collections.ObjectModel;
using meteor.Core.Interfaces.Services;
using meteor.Core.Interfaces.ViewModels;
using meteor.Core.Models.Tabs;
using meteor.UI.Factories;
using meteor.UI.ViewModels;
using meteor.UnitTests.Helpers;
using Moq;

namespace meteor.UnitTests.UI.ViewModels;

public class TabViewModelTests
{
    private readonly Mock<ITabService> _mockTabService;
    private readonly Mock<IEditorViewModelFactory> _mockEditorViewModelFactory;
    private readonly Mock<ICommandFactory> _mockCommandFactory;
    private readonly TabViewModel _tabViewModel;

    public TabViewModelTests()
    {
        _mockTabService = new Mock<ITabService>();
        _mockEditorViewModelFactory = new Mock<IEditorViewModelFactory>();
        _mockCommandFactory = new Mock<ICommandFactory>();

        SetupCommandFactory();

        _tabViewModel = new TabViewModel(_mockEditorViewModelFactory.Object, _mockCommandFactory.Object,
            _mockTabService.Object);
    }

    private void SetupCommandFactory()
    {
        _mockCommandFactory.Setup(f => f.CreateCommand(It.IsAny<Action>()))
            .Returns((Action action) => new DelegateCommand(action));
        _mockCommandFactory.Setup(f => f.CreateCommand(It.IsAny<Action<ITabItemViewModel>>()))
            .Returns((Action<ITabItemViewModel> action) => new DelegateCommand<ITabItemViewModel>(action));
    }

    [Fact]
    public void CloseOtherTabs_KeepsSpecifiedTab_ClosesOthers()
    {
        // Arrange
        var tabToKeep = CreateMockTabItemViewModel(1, "Tab 1");
        var tabToClose1 = CreateMockTabItemViewModel(2, "Tab 2");
        var tabToClose2 = CreateMockTabItemViewModel(3, "Tab 3");

        _tabViewModel.Tabs = new ObservableCollection<ITabItemViewModel?>
        {
            tabToKeep,
            tabToClose1,
            tabToClose2
        };

        _mockTabService.Setup(s => s.CloseOtherTabs(1)).Verifiable();

        // Act
        _tabViewModel.CloseOtherTabsCommand.Execute(tabToKeep);

        // Assert
        Assert.Single(_tabViewModel.Tabs);
        Assert.Equal(tabToKeep, _tabViewModel.Tabs.First());
        Assert.Equal(tabToKeep, _tabViewModel.SelectedTab);

        _mockTabService.Verify(s => s.CloseOtherTabs(1), Times.Once);
    }

    [Fact]
    public void CloseOtherTabs_KeepsNonSelectedTab_UpdatesSelectedTab()
    {
        // Arrange
        var selectedTab = CreateMockTabItemViewModel(1, "Tab 1");
        var tabToKeep = CreateMockTabItemViewModel(2, "Tab 2");
        var tabToClose = CreateMockTabItemViewModel(3, "Tab 3");

        _tabViewModel.Tabs = new ObservableCollection<ITabItemViewModel?>
        {
            selectedTab,
            tabToKeep,
            tabToClose
        };
        _tabViewModel.SelectedTab = selectedTab;

        _mockTabService.Setup(s => s.GetAllTabs()).Returns(new List<TabInfo>
        {
            new(2, "Tab 2", new Mock<ITextBufferService>().Object)
        });

        // Act
        _tabViewModel.CloseOtherTabsCommand.Execute(tabToKeep);

        // Assert
        Assert.Single(_tabViewModel.Tabs);
        Assert.Equal(tabToKeep, _tabViewModel.Tabs.First());
        Assert.Equal(tabToKeep, _tabViewModel.SelectedTab);

        _mockTabService.Verify(s => s.CloseOtherTabs(2), Times.Once);
    }

    [Fact]
    public void CloseOtherTabs_WithNoTabs_DoesNothing()
    {
        // Arrange
        _tabViewModel.Tabs.Clear();

        // Act
        _tabViewModel.CloseOtherTabsCommand.Execute(null);

        // Assert
        Assert.Empty(_tabViewModel.Tabs);
        Assert.Null(_tabViewModel.SelectedTab);

        _mockTabService.Verify(s => s.CloseOtherTabs(It.IsAny<int>()), Times.Never);
    }

    private ITabItemViewModel? CreateMockTabItemViewModel(int index, string title)
    {
        var mockTabItemViewModel = new Mock<ITabItemViewModel?>();
        mockTabItemViewModel.Setup(t => t.Index).Returns(index);
        mockTabItemViewModel.Setup(t => t.Title).Returns(title);
        return mockTabItemViewModel.Object;
    }
}