namespace meteor.Core.Interfaces;

public interface ICursorPositionHandler
{
    int Position { get; }
    void SetPosition(int position);
}