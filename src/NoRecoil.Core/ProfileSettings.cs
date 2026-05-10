namespace NoRecoil.Core;

public sealed class ProfileSettings
{
    public RecoilPreset Preset { get; init; } = new();
    public bool RecoilEnabled { get; init; }
    public bool RequireRightMouseButton { get; init; } = true;
    public string BluetoothPort { get; init; } = string.Empty;
}
