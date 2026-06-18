using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace LuminaUI.Controls;

public static class LuminaKeyGestureSerializer
{
    private const string CtrlModifier = "Ctrl";
    private const string ShiftModifier = "Shift";
    private const string AltModifier = "Alt";
    private const string MetaModifier = "Meta";
    private const char Separator = '+';

    private static readonly IReadOnlyDictionary<string, KeyModifiers> ModifierAliases =
        new Dictionary<string, KeyModifiers>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ctrl"] = KeyModifiers.Control,
            ["Control"] = KeyModifiers.Control,
            ["Shift"] = KeyModifiers.Shift,
            ["Alt"] = KeyModifiers.Alt,
            ["Option"] = KeyModifiers.Alt,
            ["Meta"] = KeyModifiers.Meta,
            ["Cmd"] = KeyModifiers.Meta,
            ["Command"] = KeyModifiers.Meta,
            ["Win"] = KeyModifiers.Meta,
            ["Windows"] = KeyModifiers.Meta,
            ["Super"] = KeyModifiers.Meta,
        };

    private static readonly IReadOnlyDictionary<string, Key> KeyAliases =
        new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase)
        {
            ["Esc"] = Key.Escape,
            ["Return"] = Key.Enter,
            ["Del"] = Key.Delete,
            ["Spacebar"] = Key.Space,
            ["PgUp"] = Key.PageUp,
            ["PgDn"] = Key.PageDown,
            ["Ins"] = Key.Insert,
        };

    public static string Serialize(KeyGesture? gesture)
    {
        if (gesture == null)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        KeyModifiers modifiers = gesture.KeyModifiers;
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            parts.Add(CtrlModifier);
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            parts.Add(ShiftModifier);
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            parts.Add(AltModifier);
        }

        if (modifiers.HasFlag(KeyModifiers.Meta))
        {
            parts.Add(MetaModifier);
        }

        parts.Add(SerializeKey(gesture.Key));
        return string.Join(Separator, parts);
    }

    public static KeyGesture Deserialize(string value)
    {
        if (TryDeserialize(value, out KeyGesture? gesture) && gesture != null)
        {
            return gesture;
        }

        throw new FormatException($"Invalid key gesture storage value: '{value}'.");
    }

    public static bool TryDeserialize(string? value, out KeyGesture? gesture)
    {
        gesture = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        Key key = Key.None;
        KeyModifiers modifiers = KeyModifiers.None;
        string[] parts = value.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            if (ModifierAliases.TryGetValue(part, out KeyModifiers modifier))
            {
                modifiers |= modifier;
                continue;
            }

            if (key != Key.None || !TryParseKey(part, out key))
            {
                gesture = null;
                return false;
            }
        }

        if (key == Key.None)
        {
            return false;
        }

        gesture = modifiers == KeyModifiers.None
            ? new KeyGesture(key)
            : new KeyGesture(key, modifiers);
        return true;
    }

    private static bool TryParseKey(string value, out Key key)
    {
        if (KeyAliases.TryGetValue(value, out key))
        {
            return true;
        }

        if (value.Length == 1)
        {
            char character = value[0];
            if (character >= '0' && character <= '9')
            {
                return Enum.TryParse($"D{character}", ignoreCase: true, out key);
            }
        }

        return Enum.TryParse(value, ignoreCase: true, out key) && key != Key.None;
    }

    private static string SerializeKey(Key key)
    {
        if (key >= Key.D0 && key <= Key.D9)
        {
            return ((int)(key - Key.D0)).ToString();
        }

        if (key == Key.Enter || key == Key.Return)
        {
            return "Enter";
        }

        return key.ToString();
    }
}
