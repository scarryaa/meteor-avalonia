using meteor.Core.Interfaces.Config;
using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Interfaces.Factories;

public interface ITabViewModelFactory
{
    ITabViewModel Create(IEditorViewModel editorViewModel, ITabViewModelConfig tabConfig, string fileName);
}