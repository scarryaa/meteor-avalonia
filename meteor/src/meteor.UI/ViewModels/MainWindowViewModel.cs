using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using meteor.Core.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ITabService _tabService;
    private readonly IEditorInstanceFactory _editorInstanceFactory;

    public ICommand OpenNewTabCommand { get; }

    public MainWindowViewModel(ITabService tabService, IEditorInstanceFactory editorInstanceFactory)
    {
        _tabService = tabService;
        _editorInstanceFactory = editorInstanceFactory;

        OpenNewTabCommand = new RelayCommand(OpenNewTab);
    }

    public void OpenNewTab()
    {
        var newEditorInstance = _editorInstanceFactory.Create();
        _tabService.AddTab(newEditorInstance.EditorViewModel, new TabConfig(_tabService),
            $"Untitled {_tabService.Tabs.Count + 1}");
    }
}