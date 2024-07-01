using meteor.Interfaces;
using meteor.Models;

namespace meteor.ViewModels.Design;

public class DesignTabViewModel : TabViewModel
{
    public DesignTabViewModel()
        : base(
            ServiceLocator.GetService<ICursorPositionService>(),
            ServiceLocator.GetService<IUndoRedoManager<TextState>>(),
            ServiceLocator.GetService<IFileSystemWatcherFactory>(),
            ServiceLocator.GetService<ITextBuffer>(),
            ServiceLocator.GetService<FontPropertiesViewModel>(),
            ServiceLocator.GetService<LineCountViewModel>())
    {
        Title = "Tab 1";
        IsTemporary = false;
    }
}