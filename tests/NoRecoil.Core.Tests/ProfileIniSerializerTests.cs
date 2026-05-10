using NoRecoil.Core;

namespace NoRecoil.Core.Tests;

public class ProfileIniSerializerTests
{
    [Fact]
    public void Parse_ReadsProfileValues()
    {
        const string input = """
            [Profile]
            Right=5
            Down=17
            Left=1
            Up=2
            Delay=9
            RecoilEnabled=true
            RequireRightMouseButton=false
            BluetoothPort=COM5
            EnableHotkey=116
            DisableHotkey=117
            """;

        var settings = ProfileIniSerializer.Parse(input);

        Assert.Equal(5, settings.Preset.Right);
        Assert.Equal(17, settings.Preset.Down);
        Assert.Equal(1, settings.Preset.Left);
        Assert.Equal(2, settings.Preset.Up);
        Assert.Equal(9, settings.Preset.Delay);
        Assert.True(settings.RecoilEnabled);
        Assert.False(settings.RequireRightMouseButton);
        Assert.Equal("COM5", settings.BluetoothPort);
        Assert.Equal(116, settings.EnableHotkey);
        Assert.Equal(117, settings.DisableHotkey);
    }

    [Fact]
    public void Parse_UsesDefaultsForMissingValues()
    {
        const string input = """
            [Profile]
            Right=3
            """;

        var settings = ProfileIniSerializer.Parse(input);

        Assert.Equal(3, settings.Preset.Right);
        Assert.Equal(0, settings.Preset.Down);
        Assert.False(settings.RecoilEnabled);
        Assert.True(settings.RequireRightMouseButton);
        Assert.Equal(string.Empty, settings.BluetoothPort);
        Assert.Equal(ProfileSettings.DefaultEnableHotkey, settings.EnableHotkey);
        Assert.Equal(ProfileSettings.DefaultDisableHotkey, settings.DisableHotkey);
    }

    [Fact]
    public void Serialize_WritesAllProfileKeys()
    {
        var settings = new ProfileSettings
        {
            Preset = new RecoilPreset
            {
                Right = 4,
                Down = 11,
                Left = 2,
                Up = 0,
                Delay = 3,
            },
            RecoilEnabled = true,
            RequireRightMouseButton = true,
            BluetoothPort = "COM7",
            EnableHotkey = 114,
            DisableHotkey = 115,
        };

        var ini = ProfileIniSerializer.Serialize(settings, includeSectionHeader: true);

        Assert.Contains("[Profile]", ini);
        Assert.Contains("Right=4", ini);
        Assert.Contains("Down=11", ini);
        Assert.Contains("Left=2", ini);
        Assert.Contains("Up=0", ini);
        Assert.Contains("Delay=3", ini);
        Assert.Contains("RecoilEnabled=True", ini);
        Assert.Contains("RequireRightMouseButton=True", ini);
        Assert.Contains("BluetoothPort=COM7", ini);
        Assert.Contains("EnableHotkey=114", ini);
        Assert.Contains("DisableHotkey=115", ini);
    }
}
