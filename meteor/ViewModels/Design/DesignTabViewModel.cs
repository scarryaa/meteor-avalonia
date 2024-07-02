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
            ServiceLocator.GetService<ITextBufferFactory>(),
            ServiceLocator.GetService<FontPropertiesViewModel>(),
            ServiceLocator.GetService<LineCountViewModel>(),
            ServiceLocator.GetService<IClipboardService>(),
            ServiceLocator.GetService<IAutoSaveService>(),
            ServiceLocator.GetService<IThemeService>())
    {
        Title = "Tab 1";
        IsTemporary = false;
    }
}