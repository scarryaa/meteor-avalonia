using Key = meteor.Core.Enums.Key;
using KeyModifiers = meteor.Core.Enums.KeyModifiers;

namespace meteor.UI.Common.Adapters;

public static class KeyConverter
{
    public static Key Convert(Avalonia.Input.Key key)
    {
        return key switch
        {
            Avalonia.Input.Key.None => Key.None,
            Avalonia.Input.Key.Back => Key.Back,
            Avalonia.Input.Key.Tab => Key.Tab,
            Avalonia.Input.Key.Enter => Key.Enter,
            Avalonia.Input.Key.Escape => Key.Escape,
            Avalonia.Input.Key.Space => Key.Space,
            Avalonia.Input.Key.Left => Key.Left,
            Avalonia.Input.Key.Up => Key.Up,
            Avalonia.Input.Key.Right => Key.Right,
            Avalonia.Input.Key.Down => Key.Down,
            Avalonia.Input.Key.Delete => Key.Delete,
            Avalonia.Input.Key.Insert => Key.Insert,
            Avalonia.Input.Key.Home => Key.Home,
            Avalonia.Input.Key.End => Key.End,
            Avalonia.Input.Key.PageUp => Key.PageUp,
            Avalonia.Input.Key.PageDown => Key.PageDown,
            Avalonia.Input.Key.CapsLock => Key.CapsLock,
            Avalonia.Input.Key.Scroll => Key.ScrollLock,
            Avalonia.Input.Key.NumLock => Key.NumLock,
            Avalonia.Input.Key.PrintScreen => Key.PrintScreen,
            Avalonia.Input.Key.Pause => Key.Pause,
            Avalonia.Input.Key.LeftShift => Key.LeftShift,
            Avalonia.Input.Key.RightShift => Key.RightShift,
            Avalonia.Input.Key.LeftCtrl => Key.LeftCtrl,
            Avalonia.Input.Key.RightCtrl => Key.RightCtrl,
            Avalonia.Input.Key.LeftAlt => Key.LeftAlt,
            Avalonia.Input.Key.RightAlt => Key.RightAlt,
            Avalonia.Input.Key.LWin => Key.LeftMeta,
            Avalonia.Input.Key.RWin => Key.RightMeta,
            Avalonia.Input.Key.MediaMenu => Key.Menu,
            Avalonia.Input.Key.F1 => Key.F1,
            Avalonia.Input.Key.F2 => Key.F2,
            Avalonia.Input.Key.F3 => Key.F3,
            Avalonia.Input.Key.F4 => Key.F4,
            Avalonia.Input.Key.F5 => Key.F5,
            Avalonia.Input.Key.F6 => Key.F6,
            Avalonia.Input.Key.F7 => Key.F7,
            Avalonia.Input.Key.F8 => Key.F8,
            Avalonia.Input.Key.F9 => Key.F9,
            Avalonia.Input.Key.F10 => Key.F10,
            Avalonia.Input.Key.F11 => Key.F11,
            Avalonia.Input.Key.F12 => Key.F12,
            Avalonia.Input.Key.F13 => Key.F13,
            Avalonia.Input.Key.F14 => Key.F14,
            Avalonia.Input.Key.F15 => Key.F15,
            Avalonia.Input.Key.F16 => Key.F16,
            Avalonia.Input.Key.F17 => Key.F17,
            Avalonia.Input.Key.F18 => Key.F18,
            Avalonia.Input.Key.F19 => Key.F19,
            Avalonia.Input.Key.F20 => Key.F20,
            Avalonia.Input.Key.F21 => Key.F21,
            Avalonia.Input.Key.F22 => Key.F22,
            Avalonia.Input.Key.F23 => Key.F23,
            Avalonia.Input.Key.F24 => Key.F24,
            Avalonia.Input.Key.NumPad0 => Key.NumPad0,
            Avalonia.Input.Key.NumPad1 => Key.NumPad1,
            Avalonia.Input.Key.NumPad2 => Key.NumPad2,
            Avalonia.Input.Key.NumPad3 => Key.NumPad3,
            Avalonia.Input.Key.NumPad4 => Key.NumPad4,
            Avalonia.Input.Key.NumPad5 => Key.NumPad5,
            Avalonia.Input.Key.NumPad6 => Key.NumPad6,
            Avalonia.Input.Key.NumPad7 => Key.NumPad7,
            Avalonia.Input.Key.NumPad8 => Key.NumPad8,
            Avalonia.Input.Key.NumPad9 => Key.NumPad9,
            Avalonia.Input.Key.Multiply => Key.NumPadMultiply,
            Avalonia.Input.Key.Add => Key.NumPadAdd,
            Avalonia.Input.Key.Subtract => Key.NumPadSubtract,
            Avalonia.Input.Key.Decimal => Key.NumPadDecimal,
            Avalonia.Input.Key.Divide => Key.NumPadDivide,
            Avalonia.Input.Key.A => Key.A,
            Avalonia.Input.Key.B => Key.B,
            Avalonia.Input.Key.C => Key.C,
            Avalonia.Input.Key.D => Key.D,
            Avalonia.Input.Key.E => Key.E,
            Avalonia.Input.Key.F => Key.F,
            Avalonia.Input.Key.G => Key.G,
            Avalonia.Input.Key.H => Key.H,
            Avalonia.Input.Key.I => Key.I,
            Avalonia.Input.Key.J => Key.J,
            Avalonia.Input.Key.K => Key.K,
            Avalonia.Input.Key.L => Key.L,
            Avalonia.Input.Key.M => Key.M,
            Avalonia.Input.Key.N => Key.N,
            Avalonia.Input.Key.O => Key.O,
            Avalonia.Input.Key.P => Key.P,
            Avalonia.Input.Key.Q => Key.Q,
            Avalonia.Input.Key.R => Key.R,
            Avalonia.Input.Key.S => Key.S,
            Avalonia.Input.Key.T => Key.T,
            Avalonia.Input.Key.U => Key.U,
            Avalonia.Input.Key.V => Key.V,
            Avalonia.Input.Key.W => Key.W,
            Avalonia.Input.Key.X => Key.X,
            Avalonia.Input.Key.Y => Key.Y,
            Avalonia.Input.Key.Z => Key.Z,
            Avalonia.Input.Key.D0 => Key.D0,
            Avalonia.Input.Key.D1 => Key.D1,
            Avalonia.Input.Key.D2 => Key.D2,
            Avalonia.Input.Key.D3 => Key.D3,
            Avalonia.Input.Key.D4 => Key.D4,
            Avalonia.Input.Key.D5 => Key.D5,
            Avalonia.Input.Key.D6 => Key.D6,
            Avalonia.Input.Key.D7 => Key.D7,
            Avalonia.Input.Key.D8 => Key.D8,
            Avalonia.Input.Key.D9 => Key.D9,
            Avalonia.Input.Key.OemMinus => Key.Minus,
            Avalonia.Input.Key.OemPlus => Key.Equal,
            Avalonia.Input.Key.OemComma => Key.Comma,
            Avalonia.Input.Key.OemPeriod => Key.Period,
            Avalonia.Input.Key.MediaNextTrack => Key.MediaNextTrack,
            Avalonia.Input.Key.MediaPreviousTrack => Key.MediaPreviousTrack,
            Avalonia.Input.Key.MediaStop => Key.MediaStop,
            Avalonia.Input.Key.MediaPlayPause => Key.MediaPlayPause,
            Avalonia.Input.Key.VolumeMute => Key.VolumeMute,
            Avalonia.Input.Key.VolumeUp => Key.VolumeUp,
            Avalonia.Input.Key.VolumeDown => Key.VolumeDown,
            Avalonia.Input.Key.Oem1 => Key.Oem1,
            Avalonia.Input.Key.Oem2 => Key.Oem2,
            Avalonia.Input.Key.Oem3 => Key.Oem3,
            Avalonia.Input.Key.Oem4 => Key.Oem4,
            Avalonia.Input.Key.Oem5 => Key.Oem5,
            Avalonia.Input.Key.Oem6 => Key.Oem6,
            Avalonia.Input.Key.Oem7 => Key.Oem7,
            Avalonia.Input.Key.Oem8 => Key.Oem8,
            Avalonia.Input.Key.Oem102 => Key.Oem102,
            _ => Key.None
        };
    }

    public static KeyModifiers Convert(Avalonia.Input.KeyModifiers keyModifiers)
    {
        var result = KeyModifiers.None;

        if (keyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt))
            result |= KeyModifiers.Alt;

        if (keyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
            result |= KeyModifiers.Control;

        if (keyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift))
            result |= KeyModifiers.Shift;

        if (keyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Meta))
            result |= KeyModifiers.Meta;

        return result;
    }
}