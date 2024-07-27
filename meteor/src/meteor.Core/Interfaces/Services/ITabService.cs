using System.Collections.ObjectModel;
using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Interfaces.Services;

public interface ITabService
{
    ObservableCollection<ITabViewModel> Tabs { get; }
    ITabViewModel ActiveTab { get; }
    void AddTab(IEditorViewModel editorViewModel, ITabViewModelConfig tabConfig, string fileName);
    void RemoveTab(ITabViewModel tab);
    void SetActiveTab(ITabViewModel tab);

    event EventHandler<ITabViewModel> TabAdded;
    event EventHandler<ITabViewModel> TabRemoved;
    event EventHandler<ITabViewModel> ActiveTabChanged;
}