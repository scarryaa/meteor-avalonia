using meteor.Core.Enums;

namespace meteor.Core.Models.Events;

public class PointerEventArgs
{
    public PointerEventArgs()
    {
    }

    public PointerEventArgs(double x, double y, int index, KeyModifiers modifiers = KeyModifiers.None,
        bool isLeftButtonPressed = false, bool isRightButtonPressed = false, bool isMiddleButtonPressed = false)
    {
        X = x;
        Y = y;
        Index = index;
        Modifiers = modifiers;
        IsLeftButtonPressed = isLeftButtonPressed;
        IsRightButtonPressed = isRightButtonPressed;
        IsMiddleButtonPressed = isMiddleButtonPressed;
    }

    public double X { get; init; }
    public double Y { get; init; }
    public int Index { get; init; }
    public KeyModifiers Modifiers { get; init; }
    public bool IsLeftButtonPressed { get; init; }
    public bool IsRightButtonPressed { get; init; }
    public bool IsMiddleButtonPressed { get; init; }
}