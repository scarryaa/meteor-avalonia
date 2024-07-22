using meteor.Core.Enums;

namespace meteor.Core.Models.Events;

public class PointerReleasedEventArgs
{
    public PointerReleasedEventArgs()
    {
    }

    public PointerReleasedEventArgs(int index, double x, double y, KeyModifiers modifiers = KeyModifiers.None,
        bool isLeftButtonPressed = false, bool isRightButtonPressed = false, bool isMiddleButtonPressed = false)
    {
        Index = index;
        X = x;
        Y = y;
        Modifiers = modifiers;
        IsLeftButtonPressed = isLeftButtonPressed;
        IsRightButtonPressed = isRightButtonPressed;
        IsMiddleButtonPressed = isMiddleButtonPressed;
    }

    public int Index { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public KeyModifiers Modifiers { get; init; }
    public bool IsLeftButtonPressed { get; init; }
    public bool IsRightButtonPressed { get; init; }
    public bool IsMiddleButtonPressed { get; init; }
}