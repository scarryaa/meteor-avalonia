using meteor.Core.Interfaces.ViewModels;
using meteor.UI.Features.Editor.Controls;

namespace meteor.UI.Features.Editor.Interfaces;

public interface IEditorControlFactory
{
    EditorControl CreateEditorControl(IEditorViewModel viewModel);
}