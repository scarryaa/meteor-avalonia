namespace meteor.ViewModels;

public class TabViewModel
{
    public string Title { get; set; }
    public string FilePath { get; set; }
    public TextEditorViewModel TextEditorViewModel { get; set; }
    public bool IsDirty { get; set; }
}