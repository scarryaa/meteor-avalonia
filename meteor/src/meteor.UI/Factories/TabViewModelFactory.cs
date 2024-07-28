using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.Factories;
using meteor.Core.Interfaces.ViewModels;
using meteor.UI.ViewModels;

namespace meteor.UI.Factories;

public class TabViewModelFactory : ITabViewModelFactory
{
    public ITabViewModel? Create(IEditorViewModel editorViewModel, ITabViewModelConfig tabConfig, string filePath,
        string fileName)
    {
        return new TabViewModel(editorViewModel, filePath, fileName, tabConfig);
    }
}