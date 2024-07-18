namespace meteor.Core.Models.Events;

public class TextInputEventArgs
{
    public string Text { get; set; }

    public TextInputEventArgs()
    {
    }

    public TextInputEventArgs(string text)
    {
        Text = text;
    }
}