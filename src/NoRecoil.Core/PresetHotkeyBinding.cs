namespace NoRecoil.Core;

public sealed class PresetHotkeyBinding
{
    public HotkeyBinding Hotkey { get; init; } = new();
    public string PresetPath { get; init; } = string.Empty;
}
