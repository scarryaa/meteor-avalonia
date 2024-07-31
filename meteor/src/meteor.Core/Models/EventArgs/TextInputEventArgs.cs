namespace meteor.Core.Models.EventArgs;

public class TextInputEventArgs : System.EventArgs
{
    public TextInputEventArgs(string text)
    {
        Text = text;
    }

    public TextInputEventArgs()
    {
    }

    public string Text { get; init; }
    public bool Handled { get; set; } = false;
}