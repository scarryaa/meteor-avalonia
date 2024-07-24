namespace meteor.Core.Models.EventArgs;

public class TextInputEventArgs : System.EventArgs
{
    public string Text { get; }

    public TextInputEventArgs(string text)
    {
        Text = text;
    }
}