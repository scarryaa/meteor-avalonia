namespace meteor.Core.Models.EventArgs;

public class TextInputEventArgs : System.EventArgs
{
    public string Text { get; init; }
    public bool Handled { get; set; } = false;

    public TextInputEventArgs(string text)
    {
        Text = text;
    }

    public TextInputEventArgs()
    {
    }
}