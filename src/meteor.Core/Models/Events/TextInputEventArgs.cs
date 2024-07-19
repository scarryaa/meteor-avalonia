namespace meteor.Core.Models.Events;

public class TextInputEventArgs
{
    public TextInputEventArgs()
    {
    }

    public TextInputEventArgs(string text)
    {
        Text = text;
    }

    public string Text { get; set; }
}