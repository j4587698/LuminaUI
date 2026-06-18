using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

namespace LuminaUI.Controls;

public static class LuminaKeyGestureDetector
{
    public static bool TryCreateGesture(KeyEventArgs args, IEnumerable<Key>? acceptableKeys, bool considerKeyModifiers, out KeyGesture? gesture)
    {
        gesture = null;

        Key key = ResolveKey(args);
        if (key == Key.None || IsModifierKey(key))
        {
            return false;
        }

        if (acceptableKeys != null && !acceptableKeys.Contains(key))
        {
            return false;
        }

        if (!considerKeyModifiers)
        {
            if (IsModifierKey(key))
            {
                return false;
            }

            gesture = new KeyGesture(key);
            return true;
        }

        gesture = new KeyGesture(key, args.KeyModifiers);
        return true;
    }

    public static bool Matches(KeyGesture? gesture, KeyEventArgs args)
    {
        if (gesture == null)
        {
            return false;
        }

        if (gesture.Matches(args))
        {
            return true;
        }

        Key key = ResolveKey(args);
        return key != Key.None
            && !IsModifierKey(key)
            && key == gesture.Key
            && args.KeyModifiers == gesture.KeyModifiers;
    }

    public static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl
            or Key.RightCtrl
            or Key.LeftAlt
            or Key.RightAlt
            or Key.LeftShift
            or Key.RightShift
            or Key.LWin
            or Key.RWin;
    }

    private static Key ResolveKey(KeyEventArgs args)
    {
        Key key = args.Key;
        if (!IsProcessedKey(key))
        {
            return key;
        }

        key = GetKeyFromPhysicalKey(args.PhysicalKey);
        return key != Key.None ? key : GetKeyFromSymbol(args.KeySymbol);
    }

    private static bool IsProcessedKey(Key key)
    {
        return key is Key.ImeProcessed
            or Key.DeadCharProcessed
            or Key.System;
    }

    private static Key GetKeyFromPhysicalKey(PhysicalKey physicalKey)
    {
        return physicalKey switch
        {
            PhysicalKey.A => Key.A,
            PhysicalKey.B => Key.B,
            PhysicalKey.C => Key.C,
            PhysicalKey.D => Key.D,
            PhysicalKey.E => Key.E,
            PhysicalKey.F => Key.F,
            PhysicalKey.G => Key.G,
            PhysicalKey.H => Key.H,
            PhysicalKey.I => Key.I,
            PhysicalKey.J => Key.J,
            PhysicalKey.K => Key.K,
            PhysicalKey.L => Key.L,
            PhysicalKey.M => Key.M,
            PhysicalKey.N => Key.N,
            PhysicalKey.O => Key.O,
            PhysicalKey.P => Key.P,
            PhysicalKey.Q => Key.Q,
            PhysicalKey.R => Key.R,
            PhysicalKey.S => Key.S,
            PhysicalKey.T => Key.T,
            PhysicalKey.U => Key.U,
            PhysicalKey.V => Key.V,
            PhysicalKey.W => Key.W,
            PhysicalKey.X => Key.X,
            PhysicalKey.Y => Key.Y,
            PhysicalKey.Z => Key.Z,
            PhysicalKey.Digit0 => Key.D0,
            PhysicalKey.Digit1 => Key.D1,
            PhysicalKey.Digit2 => Key.D2,
            PhysicalKey.Digit3 => Key.D3,
            PhysicalKey.Digit4 => Key.D4,
            PhysicalKey.Digit5 => Key.D5,
            PhysicalKey.Digit6 => Key.D6,
            PhysicalKey.Digit7 => Key.D7,
            PhysicalKey.Digit8 => Key.D8,
            PhysicalKey.Digit9 => Key.D9,
            PhysicalKey.F1 => Key.F1,
            PhysicalKey.F2 => Key.F2,
            PhysicalKey.F3 => Key.F3,
            PhysicalKey.F4 => Key.F4,
            PhysicalKey.F5 => Key.F5,
            PhysicalKey.F6 => Key.F6,
            PhysicalKey.F7 => Key.F7,
            PhysicalKey.F8 => Key.F8,
            PhysicalKey.F9 => Key.F9,
            PhysicalKey.F10 => Key.F10,
            PhysicalKey.F11 => Key.F11,
            PhysicalKey.F12 => Key.F12,
            PhysicalKey.F13 => Key.F13,
            PhysicalKey.F14 => Key.F14,
            PhysicalKey.F15 => Key.F15,
            PhysicalKey.F16 => Key.F16,
            PhysicalKey.F17 => Key.F17,
            PhysicalKey.F18 => Key.F18,
            PhysicalKey.F19 => Key.F19,
            PhysicalKey.F20 => Key.F20,
            PhysicalKey.F21 => Key.F21,
            PhysicalKey.F22 => Key.F22,
            PhysicalKey.F23 => Key.F23,
            PhysicalKey.F24 => Key.F24,
            PhysicalKey.Enter or PhysicalKey.NumPadEnter => Key.Enter,
            PhysicalKey.Escape => Key.Escape,
            PhysicalKey.Backspace => Key.Back,
            PhysicalKey.Tab => Key.Tab,
            PhysicalKey.Space => Key.Space,
            PhysicalKey.Delete => Key.Delete,
            PhysicalKey.Insert => Key.Insert,
            PhysicalKey.Home => Key.Home,
            PhysicalKey.End => Key.End,
            PhysicalKey.PageUp => Key.PageUp,
            PhysicalKey.PageDown => Key.PageDown,
            PhysicalKey.ArrowLeft => Key.Left,
            PhysicalKey.ArrowUp => Key.Up,
            PhysicalKey.ArrowRight => Key.Right,
            PhysicalKey.ArrowDown => Key.Down,
            PhysicalKey.NumPad0 => Key.NumPad0,
            PhysicalKey.NumPad1 => Key.NumPad1,
            PhysicalKey.NumPad2 => Key.NumPad2,
            PhysicalKey.NumPad3 => Key.NumPad3,
            PhysicalKey.NumPad4 => Key.NumPad4,
            PhysicalKey.NumPad5 => Key.NumPad5,
            PhysicalKey.NumPad6 => Key.NumPad6,
            PhysicalKey.NumPad7 => Key.NumPad7,
            PhysicalKey.NumPad8 => Key.NumPad8,
            PhysicalKey.NumPad9 => Key.NumPad9,
            PhysicalKey.NumPadAdd => Key.Add,
            PhysicalKey.NumPadSubtract => Key.Subtract,
            PhysicalKey.NumPadMultiply => Key.Multiply,
            PhysicalKey.NumPadDivide => Key.Divide,
            PhysicalKey.NumPadDecimal => Key.Decimal,
            PhysicalKey.Backquote => Key.OemTilde,
            PhysicalKey.Minus => Key.OemMinus,
            PhysicalKey.Equal => Key.OemPlus,
            PhysicalKey.BracketLeft => Key.OemOpenBrackets,
            PhysicalKey.BracketRight => Key.OemCloseBrackets,
            PhysicalKey.Backslash => Key.OemPipe,
            PhysicalKey.IntlBackslash => Key.Oem102,
            PhysicalKey.Semicolon => Key.OemSemicolon,
            PhysicalKey.Quote => Key.OemQuotes,
            PhysicalKey.Comma => Key.OemComma,
            PhysicalKey.Period => Key.OemPeriod,
            PhysicalKey.Slash => Key.OemQuestion,
            PhysicalKey.ControlLeft => Key.LeftCtrl,
            PhysicalKey.ControlRight => Key.RightCtrl,
            PhysicalKey.AltLeft => Key.LeftAlt,
            PhysicalKey.AltRight => Key.RightAlt,
            PhysicalKey.ShiftLeft => Key.LeftShift,
            PhysicalKey.ShiftRight => Key.RightShift,
            PhysicalKey.MetaLeft => Key.LWin,
            PhysicalKey.MetaRight => Key.RWin,
            _ => Key.None,
        };
    }

    private static Key GetKeyFromSymbol(string? symbol)
    {
        if (string.IsNullOrEmpty(symbol) || symbol.Length != 1)
        {
            return Key.None;
        }

        char value = symbol[0];
        if (value is >= 'a' and <= 'z')
        {
            return ParseKey(value.ToString().ToUpperInvariant());
        }

        if (value is >= 'A' and <= 'Z')
        {
            return ParseKey(value.ToString());
        }

        return value switch
        {
            '0' => Key.D0,
            '1' => Key.D1,
            '2' => Key.D2,
            '3' => Key.D3,
            '4' => Key.D4,
            '5' => Key.D5,
            '6' => Key.D6,
            '7' => Key.D7,
            '8' => Key.D8,
            '9' => Key.D9,
            ' ' => Key.Space,
            '`' or '~' => Key.OemTilde,
            '-' or '_' => Key.OemMinus,
            '=' or '+' => Key.OemPlus,
            '[' or '{' => Key.OemOpenBrackets,
            ']' or '}' => Key.OemCloseBrackets,
            '\\' or '|' => Key.OemPipe,
            ';' or ':' => Key.OemSemicolon,
            '\'' or '"' => Key.OemQuotes,
            ',' or '<' => Key.OemComma,
            '.' or '>' => Key.OemPeriod,
            '/' or '?' => Key.OemQuestion,
            _ => Key.None,
        };
    }

    private static Key ParseKey(string value)
    {
        return System.Enum.TryParse(value, out Key key) ? key : Key.None;
    }
}
