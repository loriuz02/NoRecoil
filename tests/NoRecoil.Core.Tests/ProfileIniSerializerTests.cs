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
            Left=-1
            Up=2
            Delay=9
            RecoilEnabled=true
            RequireRightMouseButton=false
            BluetoothPort=COM5
            EnableHotkey=Ctrl+116
            DisableHotkey=Shift+117
            PresetHotkey1=Shift+56|C:\presets\ak.ini
            PresetHotkey2=18+57|C:\presets\m4.ini
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
        Assert.Equal(116, settings.EnableHotkey.Key);
        Assert.Equal(HotkeyModifiers.Control, settings.EnableHotkey.Modifiers);
        Assert.Equal(117, settings.DisableHotkey.Key);
        Assert.Equal(HotkeyModifiers.Shift, settings.DisableHotkey.Modifiers);
        Assert.Equal(2, settings.PresetHotkeys.Count);
        Assert.Equal(56, settings.PresetHotkeys[0].Hotkey.Key);
        Assert.Equal(HotkeyModifiers.Shift, settings.PresetHotkeys[0].Hotkey.Modifiers);
        Assert.Equal(@"C:\presets\ak.ini", settings.PresetHotkeys[0].PresetPath);
        Assert.Equal(57, settings.PresetHotkeys[1].Hotkey.Key);
        Assert.Equal(HotkeyModifiers.None, settings.PresetHotkeys[1].Hotkey.Modifiers);
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
        Assert.Equal(ProfileSettings.DefaultEnableHotkeyKey, settings.EnableHotkey.Key);
        Assert.Equal(ProfileSettings.DefaultDisableHotkeyKey, settings.DisableHotkey.Key);
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
            EnableHotkey = new HotkeyBinding
            {
                Key = 114,
                Modifiers = HotkeyModifiers.Control,
            },
            DisableHotkey = new HotkeyBinding
            {
                Key = 115,
                Modifiers = HotkeyModifiers.Shift | HotkeyModifiers.Alt,
            },
            PresetHotkeys =
            [
                new PresetHotkeyBinding
                {
                    Hotkey = new HotkeyBinding
                    {
                        Key = 56,
                        Modifiers = HotkeyModifiers.Shift,
                    },
                    PresetPath = @"C:\presets\ak.ini",
                },
            ],
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
        Assert.Contains("EnableHotkey=Ctrl+114", ini);
        Assert.Contains("DisableHotkey=Alt+Shift+115", ini);
        Assert.Contains(@"PresetHotkey1=Shift+56|C:\presets\ak.ini", ini);
    }

    [Fact]
    public void Parse_LimitsPresetHotkeysToMax()
    {
        const int excessivePresetHotkeyCount = ProfileSettings.MaxPresetHotkeys + 5;
        var lines = new List<string>
        {
            "[Profile]",
            "Right=1",
        };

        for (var i = 1; i <= excessivePresetHotkeyCount; i++)
        {
            lines.Add($"PresetHotkey{i}=Ctrl+{80 + i}|C:\\presets\\preset{i}.ini");
        }

        var settings = ProfileIniSerializer.Parse(string.Join(Environment.NewLine, lines));

        Assert.Equal(ProfileSettings.MaxPresetHotkeys, settings.PresetHotkeys.Count);
        Assert.Equal(@"C:\presets\preset1.ini", settings.PresetHotkeys[0].PresetPath);
        Assert.Equal(@"C:\presets\preset20.ini", settings.PresetHotkeys[^1].PresetPath);
    }

    [Fact]
    public void Serialize_LimitsPresetHotkeysToMax()
    {
        const int excessivePresetHotkeyCount = ProfileSettings.MaxPresetHotkeys + 5;
        var presetHotkeys = Enumerable.Range(1, excessivePresetHotkeyCount)
            .Select(i => new PresetHotkeyBinding
            {
                Hotkey = new HotkeyBinding
                {
                    Key = 40 + i,
                    Modifiers = HotkeyModifiers.Control,
                },
                PresetPath = $@"C:\presets\preset{i}.ini",
            })
            .ToArray();

        var settings = new ProfileSettings
        {
            Preset = new RecoilPreset(),
            PresetHotkeys = presetHotkeys,
        };

        var ini = ProfileIniSerializer.Serialize(settings, includeSectionHeader: true);

        var presetLines = ini
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("PresetHotkey", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Equal(ProfileSettings.MaxPresetHotkeys, presetLines.Length);
        Assert.Contains(@"PresetHotkey20=Ctrl+60|C:\presets\preset20.ini", presetLines);
        Assert.DoesNotContain(presetLines, line => line.StartsWith("PresetHotkey21=", StringComparison.OrdinalIgnoreCase));
    }
}
