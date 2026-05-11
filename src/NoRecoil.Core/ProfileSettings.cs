namespace NoRecoil.Core;

public sealed class ProfileSettings
{
    public const int DefaultEnableHotkeyKey = 0x70; // F1
    public const int DefaultDisableHotkeyKey = 0x71; // F2

    public RecoilPreset Preset { get; init; } = new();
    public bool RecoilEnabled { get; init; }
    public bool RequireRightMouseButton { get; init; } = true;
    public string BluetoothPort { get; init; } = string.Empty;
    public HotkeyBinding EnableHotkey { get; init; } = new()
    {
        Key = DefaultEnableHotkeyKey,
        Modifiers = HotkeyModifiers.None,
    };

    public HotkeyBinding DisableHotkey { get; init; } = new()
    {
        Key = DefaultDisableHotkeyKey,
        Modifiers = HotkeyModifiers.None,
    };

    public IReadOnlyList<PresetHotkeyBinding> PresetHotkeys { get; init; } = [];
}
