using meteor.Core.Interfaces.ViewModels;

namespace meteor.Core.Interfaces.Models;

public interface IEditorInstance
{
    IEditorViewModel EditorViewModel { get; }
}