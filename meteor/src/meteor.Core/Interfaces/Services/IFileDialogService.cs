namespace meteor.Core.Interfaces.Services;

public interface IFileDialogService
{
    object TopLevelRef { get; set; }

    Task<string> ShowOpenFileDialogAsync();
    Task<string> ShowSaveFileDialogAsync();
}