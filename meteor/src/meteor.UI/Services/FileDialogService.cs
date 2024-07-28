using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using meteor.Core.Interfaces.Services;

namespace meteor.UI.Services;

public class FileDialogService : IFileDialogService
{
    public object TopLevelRef { get; set; }

    public async Task<string> ShowOpenFileDialogAsync()
    {
        EnsureTopLevelRef();
        var storageProvider = TopLevel.GetTopLevel(TopLevelRef as Visual).StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false
        });

        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    public async Task<string> ShowSaveFileDialogAsync()
    {
        EnsureTopLevelRef();
        var storageProvider = TopLevel.GetTopLevel(TopLevelRef as Visual).StorageProvider;
        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions());

        return result?.Path.LocalPath;
    }

    private void EnsureTopLevelRef()
    {
        if (TopLevelRef == null)
            throw new InvalidOperationException(
                "No top-level window reference found. Unable to perform file dialog operations.");
    }
}