namespace NoRecoil.Core;

public sealed class ProfileSettings
{
    public const int DefaultEnableHotkeyKey = 0x70; // F1
    public const int DefaultDisableHotkeyKey = 0x71; // F2
    public const int MaxHotkeys = 64;
    public const int MaxPresetHotkeys = MaxHotkeys;

    public RecoilPreset Preset { get; init; } = new();
    public bool RecoilEnabled { get; init; }
    public bool RequireRightMouseButton { get; init; } = true;
    public string BluetoothPort { get; init; } = string.Empty;
    public IReadOnlyList<PresetHotkeyBinding> Hotkeys { get; init; } = [];
}
