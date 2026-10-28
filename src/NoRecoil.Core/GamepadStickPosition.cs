namespace NoRecoil.Core;

public readonly record struct GamepadStickPosition(short X, short Y)
{
    public static GamepadStickPosition FromPreset(RecoilPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        return new GamepadStickPosition(
            Scale((long)preset.Right - preset.Left),
            Scale((long)preset.Up - preset.Down));
    }

    private static short Scale(long value)
    {
        var percentage = Math.Clamp(value, -100, 100);
        return (short)(percentage * (percentage < 0 ? 32768 : 32767) / 100);
    }
}
