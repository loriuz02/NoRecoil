namespace NoRecoil.Core;

public sealed class RecoilPresetFile
{
    public RecoilPresetFile(RecoilPreset primary, RecoilPreset? secondary = null)
    {
        Primary = primary;
        Secondary = secondary;
    }

    public RecoilPreset Primary { get; }

    public RecoilPreset? Secondary { get; }

    public bool HasSecondary => Secondary is not null;
}
