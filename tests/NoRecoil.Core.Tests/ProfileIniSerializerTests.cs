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
            Hotkey1=Ctrl+116|__action__:EnableRecoil
            Hotkey2=Shift+117|__action__:DisableRecoil
            Hotkey3=Shift+56|C:\presets\ak.ini
            Hotkey4=18+57|C:\presets\m4.ini
            Hotkey5=Ctrl+118|__action__:EnableRecoil
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
        Assert.Equal(5, settings.Hotkeys.Count);
        Assert.Equal(116, settings.Hotkeys[0].Hotkey.Key);
        Assert.Equal(HotkeyModifiers.Control, settings.Hotkeys[0].Hotkey.Modifiers);
        Assert.Equal("__action__:EnableRecoil", settings.Hotkeys[0].PresetPath);
        Assert.Equal(117, settings.Hotkeys[1].Hotkey.Key);
        Assert.Equal(HotkeyModifiers.Shift, settings.Hotkeys[1].Hotkey.Modifiers);
        Assert.Equal("__action__:DisableRecoil", settings.Hotkeys[1].PresetPath);
        Assert.Equal(56, settings.Hotkeys[2].Hotkey.Key);
        Assert.Equal(HotkeyModifiers.Shift, settings.Hotkeys[2].Hotkey.Modifiers);
        Assert.Equal(@"C:\presets\ak.ini", settings.Hotkeys[2].PresetPath);
        Assert.Equal(57, settings.Hotkeys[3].Hotkey.Key);
        Assert.Equal(HotkeyModifiers.None, settings.Hotkeys[3].Hotkey.Modifiers);
        Assert.Equal(@"C:\presets\m4.ini", settings.Hotkeys[3].PresetPath);
        Assert.Equal(118, settings.Hotkeys[4].Hotkey.Key);
        Assert.Equal(HotkeyModifiers.Control, settings.Hotkeys[4].Hotkey.Modifiers);
        Assert.Equal("__action__:EnableRecoil", settings.Hotkeys[4].PresetPath);
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
        Assert.Empty(settings.Hotkeys);
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
            Hotkeys =
            [
                new PresetHotkeyBinding
                {
                    Hotkey = new HotkeyBinding
                    {
                        Key = 114,
                        Modifiers = HotkeyModifiers.Control,
                    },
                    PresetPath = "__action__:EnableRecoil",
                },
                new PresetHotkeyBinding
                {
                    Hotkey = new HotkeyBinding
                    {
                        Key = 115,
                        Modifiers = HotkeyModifiers.Shift | HotkeyModifiers.Alt,
                    },
                    PresetPath = "__action__:DisableRecoil",
                },
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
        Assert.Contains("Hotkey1=Ctrl+114|__action__:EnableRecoil", ini);
        Assert.Contains("Hotkey2=Alt+Shift+115|__action__:DisableRecoil", ini);
        Assert.Contains(@"Hotkey3=Shift+56|C:\presets\ak.ini", ini);
        Assert.DoesNotContain("EnableHotkey=", ini);
        Assert.DoesNotContain("DisableHotkey=", ini);
    }

    [Fact]
    public void Parse_LimitsHotkeysToMax()
    {
        const int excessiveHotkeyCount = ProfileSettings.MaxHotkeys + 5;
        var lines = new List<string>
        {
            "[Profile]",
            "Right=1",
        };

        for (var i = 1; i <= excessiveHotkeyCount; i++)
        {
            lines.Add($"Hotkey{i}=Ctrl+{80 + i}|__action__:EnableRecoil");
        }

        var settings = ProfileIniSerializer.Parse(string.Join(Environment.NewLine, lines));

        Assert.Equal(ProfileSettings.MaxHotkeys, settings.Hotkeys.Count);
        Assert.Equal("__action__:EnableRecoil", settings.Hotkeys[0].PresetPath);
        Assert.Equal("__action__:EnableRecoil", settings.Hotkeys[^1].PresetPath);
    }

    [Fact]
    public void Serialize_LimitsHotkeysToMax()
    {
        const int excessiveHotkeyCount = ProfileSettings.MaxHotkeys + 5;
        var hotkeys = Enumerable.Range(1, excessiveHotkeyCount)
            .Select(i => new PresetHotkeyBinding
            {
                Hotkey = new HotkeyBinding
                {
                    Key = 40 + i,
                    Modifiers = HotkeyModifiers.Control,
                },
                PresetPath = "__action__:ToggleRecoil",
            })
            .ToArray();

        var settings = new ProfileSettings
        {
            Preset = new RecoilPreset(),
            Hotkeys = hotkeys,
        };

        var ini = ProfileIniSerializer.Serialize(settings, includeSectionHeader: true);

        var hotkeyLines = ini
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("Hotkey", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Equal(ProfileSettings.MaxHotkeys, hotkeyLines.Length);
        Assert.Contains("Hotkey20=Ctrl+60|__action__:ToggleRecoil", hotkeyLines);
        Assert.DoesNotContain(hotkeyLines, line => line.StartsWith("Hotkey65=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_SupportsLegacyEnableDisableAndPresetHotkeyKeys()
    {
        const string input = """
            [Profile]
            EnableHotkey=Ctrl+116
            DisableHotkey=Shift+117
            PresetHotkey1=Alt+56|C:\presets\legacy.ini
            """;

        var settings = ProfileIniSerializer.Parse(input);

        Assert.Equal(3, settings.Hotkeys.Count);
        Assert.Contains(settings.Hotkeys, item => item.PresetPath == @"C:\presets\legacy.ini");
        Assert.Contains(settings.Hotkeys, item => item.PresetPath == "__action__:EnableRecoil"
                                                && item.Hotkey.Key == 116
                                                && item.Hotkey.Modifiers == HotkeyModifiers.Control);
        Assert.Contains(settings.Hotkeys, item => item.PresetPath == "__action__:DisableRecoil"
                                                && item.Hotkey.Key == 117
                                                && item.Hotkey.Modifiers == HotkeyModifiers.Shift);
    }
}
