namespace NoRecoil.Core;

public sealed class ProfileSettings
{
    public const int DefaultEnableHotkey = 0x70; // F1
    public const int DefaultDisableHotkey = 0x71; // F2

    public RecoilPreset Preset { get; init; } = new();
    public bool RecoilEnabled { get; init; }
    public bool RequireRightMouseButton { get; init; } = true;
    public string BluetoothPort { get; init; } = string.Empty;
    public int EnableHotkey { get; init; } = DefaultEnableHotkey;
    public int DisableHotkey { get; init; } = DefaultDisableHotkey;
}
