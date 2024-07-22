using System;
using meteor.Core.Enums;
using Key = Avalonia.Input.Key;

namespace meteor.UI.Adapters;

public static class KeyMapper
{
    public static Core.Enums.Key ToMeteorKey(Key avaloniaKey)
    {
        return avaloniaKey switch
        {
            Key.A => Core.Enums.Key.A,
            Key.B => Core.Enums.Key.B,
            Key.C => Core.Enums.Key.C,
            Key.D => Core.Enums.Key.D,
            Key.E => Core.Enums.Key.E,
            Key.F => Core.Enums.Key.F,
            Key.G => Core.Enums.Key.G,
            Key.H => Core.Enums.Key.H,
            Key.I => Core.Enums.Key.I,
            Key.J => Core.Enums.Key.J,
            Key.K => Core.Enums.Key.K,
            Key.L => Core.Enums.Key.L,
            Key.M => Core.Enums.Key.M,
            Key.N => Core.Enums.Key.N,
            Key.O => Core.Enums.Key.O,
            Key.P => Core.Enums.Key.P,
            Key.Q => Core.Enums.Key.Q,
            Key.R => Core.Enums.Key.R,
            Key.S => Core.Enums.Key.S,
            Key.T => Core.Enums.Key.T,
            Key.U => Core.Enums.Key.U,
            Key.V => Core.Enums.Key.V,
            Key.W => Core.Enums.Key.W,
            Key.X => Core.Enums.Key.X,
            Key.Y => Core.Enums.Key.Y,
            Key.Z => Core.Enums.Key.Z,
            Key.D0 => Core.Enums.Key.D0,
            Key.D1 => Core.Enums.Key.D1,
            Key.D2 => Core.Enums.Key.D2,
            Key.D3 => Core.Enums.Key.D3,
            Key.D4 => Core.Enums.Key.D4,
            Key.D5 => Core.Enums.Key.D5,
            Key.D6 => Core.Enums.Key.D6,
            Key.D7 => Core.Enums.Key.D7,
            Key.D8 => Core.Enums.Key.D8,
            Key.D9 => Core.Enums.Key.D9,
            Key.F1 => Core.Enums.Key.F1,
            Key.F2 => Core.Enums.Key.F2,
            Key.F3 => Core.Enums.Key.F3,
            Key.F4 => Core.Enums.Key.F4,
            Key.F5 => Core.Enums.Key.F5,
            Key.F6 => Core.Enums.Key.F6,
            Key.F7 => Core.Enums.Key.F7,
            Key.F8 => Core.Enums.Key.F8,
            Key.F9 => Core.Enums.Key.F9,
            Key.F10 => Core.Enums.Key.F10,
            Key.F11 => Core.Enums.Key.F11,
            Key.F12 => Core.Enums.Key.F12,
            Key.Enter => Core.Enums.Key.Enter,
            Key.Escape => Core.Enums.Key.Escape,
            Key.Back => Core.Enums.Key.Backspace,
            Key.Tab => Core.Enums.Key.Tab,
            Key.Space => Core.Enums.Key.Space,
            Key.Up => Core.Enums.Key.Up,
            Key.Down => Core.Enums.Key.Down,
            Key.Left => Core.Enums.Key.Left,
            Key.Right => Core.Enums.Key.Right,
            Key.Insert => Core.Enums.Key.Insert,
            Key.Delete => Core.Enums.Key.Delete,
            Key.Home => Core.Enums.Key.Home,
            Key.End => Core.Enums.Key.End,
            Key.PageUp => Core.Enums.Key.PageUp,
            Key.PageDown => Core.Enums.Key.PageDown,
            Key.LeftShift => Core.Enums.Key.LeftShift,
            Key.RightShift => Core.Enums.Key.RightShift,
            Key.LeftCtrl => Core.Enums.Key.LeftCtrl,
            Key.RightCtrl => Core.Enums.Key.RightCtrl,
            Key.LeftAlt => Core.Enums.Key.LeftAlt,
            Key.RightAlt => Core.Enums.Key.RightAlt,
            Key.OemSemicolon => Core.Enums.Key.Semicolon,
            Key.OemPlus => Core.Enums.Key.Plus,
            Key.OemComma => Core.Enums.Key.Comma,
            Key.OemMinus => Core.Enums.Key.Minus,
            Key.OemPeriod => Core.Enums.Key.Period,
            Key.OemTilde => Core.Enums.Key.Tilde,
            Key.OemOpenBrackets => Core.Enums.Key.LeftBracket,
            Key.OemBackslash => Core.Enums.Key.Backslash,
            Key.OemCloseBrackets => Core.Enums.Key.RightBracket,
            Key.OemQuotes => Core.Enums.Key.Quote,
            _ => throw new ArgumentOutOfRangeException(nameof(avaloniaKey), avaloniaKey, null)
        };
    }

    public static KeyModifiers ToMeteorKeyModifiers(Avalonia.Input.KeyModifiers avaloniaModifiers)
    {
        var meteorModifiers = KeyModifiers.None;

        if (avaloniaModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift))
            meteorModifiers |= KeyModifiers.Shift;
        if (avaloniaModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
            meteorModifiers |= KeyModifiers.Ctrl;
        if (avaloniaModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt))
            meteorModifiers |= KeyModifiers.Alt;

        return meteorModifiers;
    }
}