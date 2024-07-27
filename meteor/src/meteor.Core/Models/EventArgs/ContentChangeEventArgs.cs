namespace meteor.Core.Models.EventArgs;

public class ContentChangeEventArgs : System.EventArgs
{
    public IEnumerable<TextChange> Changes { get; }

    public ContentChangeEventArgs(IEnumerable<TextChange> changes)
    {
        Changes = changes;
    }
}