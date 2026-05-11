using System.Globalization;

namespace NoRecoil.Core;

public static class HotkeyBindingSerializer
{
    public static string Serialize(HotkeyBinding hotkey)
    {
        ArgumentNullException.ThrowIfNull(hotkey);

        var parts = new List<string>();
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        parts.Add(hotkey.Key.ToString(CultureInfo.InvariantCulture));
        return string.Join("+", parts);
    }

    public static HotkeyBinding ParseOrDefault(string text, HotkeyBinding fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        var trimmed = text.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var plainKey))
        {
            return new HotkeyBinding
            {
                Key = plainKey,
                Modifiers = HotkeyModifiers.None,
            };
        }

        var pieces = trimmed.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pieces.Length == 0)
        {
            return fallback;
        }

        if (!int.TryParse(pieces[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var key))
        {
            return fallback;
        }

        var modifiers = HotkeyModifiers.None;
        foreach (var piece in pieces[..^1])
        {
            modifiers |= piece.ToLowerInvariant() switch
            {
                "ctrl" or "control" => HotkeyModifiers.Control,
                "alt" => HotkeyModifiers.Alt,
                "shift" => HotkeyModifiers.Shift,
                _ => HotkeyModifiers.None,
            };
        }

        return new HotkeyBinding
        {
            Key = key,
            Modifiers = modifiers,
        };
    }
}
