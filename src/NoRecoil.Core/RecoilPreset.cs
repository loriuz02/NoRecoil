namespace NoRecoil.Core;

public sealed class RecoilPreset
{
    public int Right { get; init; }
    public int Down { get; init; }
    public int Left { get; init; }
    public int Up { get; init; }
    public int Delay { get; init; }

    public int HorizontalDelta => Right - Left;
    public int VerticalDelta => Down - Up;
}
