using System.Collections.ObjectModel;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Interfaces.Services;

public interface ITabService
{
    ObservableCollection<ITabViewModel?> Tabs { get; }
    ITabViewModel? ActiveTab { get; }

    ITabViewModel AddTab(IEditorViewModel editorViewModel, ITabViewModelConfig tabConfig, string fileName,
        string initialContent = "");
    void RemoveTab(ITabViewModel? tab);
    void SetActiveTab(ITabViewModel? tab);
    ITabViewModel GetPreviousActiveTab();

    event EventHandler<ITabViewModel?> TabAdded;
    event EventHandler<ITabViewModel?> TabRemoved;
    event EventHandler<ITabViewModel?> ActiveTabChanged;
}