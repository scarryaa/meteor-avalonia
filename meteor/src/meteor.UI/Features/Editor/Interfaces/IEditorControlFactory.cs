using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Controls;

namespace meteor.UI.Interfaces.Factories;

public interface IEditorControlFactory
{
    EditorControl CreateEditorControl(IEditorViewModel viewModel);
}