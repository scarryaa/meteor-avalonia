namespace meteor.Core.Interfaces.Events;

public interface ITextInputEventArgs
{
    string Text { get; }
    bool Handled { get; set; }
}