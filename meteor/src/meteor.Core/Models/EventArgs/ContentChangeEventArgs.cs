namespace meteor.Core.Models.EventArgs;

public class ContentChangeEventArgs : System.EventArgs
{
    public ContentChangeEventArgs(IEnumerable<TextChange> changes)
    {
        Changes = changes;
    }

    public IEnumerable<TextChange> Changes { get; }
}