using System.Globalization;
using System.Text;

namespace NoRecoil.Core;

public static class ProfileIniSerializer
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public static ProfileSettings Parse(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var values = new Dictionary<string, string>(KeyComparer)
        {
            ["Right"] = "0",
            ["Down"] = "0",
            ["Left"] = "0",
            ["Up"] = "0",
            ["Delay"] = "0",
            ["RecoilEnabled"] = "false",
            ["RequireRightMouseButton"] = "true",
            ["BluetoothPort"] = string.Empty,
            ["EnableHotkey"] = ProfileSettings.DefaultEnableHotkeyKey.ToString(CultureInfo.InvariantCulture),
            ["DisableHotkey"] = ProfileSettings.DefaultDisableHotkeyKey.ToString(CultureInfo.InvariantCulture),
        };
        var presetHotkeyEntries = new List<string>();

        foreach (var rawLine in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawLine.StartsWith(';') || rawLine.StartsWith('#') || rawLine.StartsWith('['))
            {
                continue;
            }

            var separator = rawLine.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = rawLine[..separator].Trim();
            if (key.StartsWith("PresetHotkey", StringComparison.OrdinalIgnoreCase))
            {
                var presetValueText = separator == rawLine.Length - 1 ? string.Empty : rawLine[(separator + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(presetValueText))
                {
                    presetHotkeyEntries.Add(presetValueText);
                }

                continue;
            }

            if (!values.ContainsKey(key))
            {
                continue;
            }

            var valueText = separator == rawLine.Length - 1 ? string.Empty : rawLine[(separator + 1)..].Trim();
            values[key] = valueText;
        }

        return new ProfileSettings
        {
            Preset = new RecoilPreset
            {
                Right = ParseAbsoluteIntOrDefault(values["Right"]),
                Down = ParseAbsoluteIntOrDefault(values["Down"]),
                Left = ParseAbsoluteIntOrDefault(values["Left"]),
                Up = ParseAbsoluteIntOrDefault(values["Up"]),
                Delay = ParseIntOrDefault(values["Delay"]),
            },
            RecoilEnabled = ParseBoolOrDefault(values["RecoilEnabled"], defaultValue: false),
            RequireRightMouseButton = ParseBoolOrDefault(values["RequireRightMouseButton"], defaultValue: true),
            BluetoothPort = values["BluetoothPort"],
            EnableHotkey = HotkeyBindingSerializer.ParseOrDefault(values["EnableHotkey"], new HotkeyBinding
            {
                Key = ProfileSettings.DefaultEnableHotkeyKey,
                Modifiers = HotkeyModifiers.None,
            }),
            DisableHotkey = HotkeyBindingSerializer.ParseOrDefault(values["DisableHotkey"], new HotkeyBinding
            {
                Key = ProfileSettings.DefaultDisableHotkeyKey,
                Modifiers = HotkeyModifiers.None,
            }),
            PresetHotkeys = ParsePresetHotkeys(presetHotkeyEntries),
        };
    }

    public static string Serialize(ProfileSettings settings, bool includeSectionHeader = true)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var output = new StringBuilder();

        if (includeSectionHeader)
        {
            output.AppendLine("[Profile]");
        }

        output.AppendLine($"Right={settings.Preset.Right}");
        output.AppendLine($"Down={settings.Preset.Down}");
        output.AppendLine($"Left={settings.Preset.Left}");
        output.AppendLine($"Up={settings.Preset.Up}");
        output.AppendLine($"Delay={settings.Preset.Delay}");
        output.AppendLine($"RecoilEnabled={settings.RecoilEnabled}");
        output.AppendLine($"RequireRightMouseButton={settings.RequireRightMouseButton}");
        output.AppendLine($"BluetoothPort={settings.BluetoothPort}");
        output.AppendLine($"EnableHotkey={HotkeyBindingSerializer.Serialize(settings.EnableHotkey)}");
        output.AppendLine($"DisableHotkey={HotkeyBindingSerializer.Serialize(settings.DisableHotkey)}");

        for (var i = 0; i < settings.PresetHotkeys.Count; i++)
        {
            var entry = settings.PresetHotkeys[i];
            var hotkey = HotkeyBindingSerializer.Serialize(entry.Hotkey);
            output.AppendLine($"PresetHotkey{i + 1}={hotkey}|{entry.PresetPath}");
        }

        return output.ToString();
    }

    public static ProfileSettings LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Parse(File.ReadAllText(path));
    }

    public static void SaveToFile(string path, ProfileSettings settings, bool includeSectionHeader = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(settings);

        File.WriteAllText(path, Serialize(settings, includeSectionHeader));
    }

    private static int ParseIntOrDefault(string valueText)
    {
        return int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static int ParseAbsoluteIntOrDefault(string valueText)
    {
        if (!int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return 0;
        }

        return NumericValueNormalizer.ToAbsolute(value);
    }

    private static bool ParseBoolOrDefault(string valueText, bool defaultValue)
    {
        if (bool.TryParse(valueText, out var parsedBool))
        {
            return parsedBool;
        }

        if (int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
        {
            return parsedInt != 0;
        }

        return valueText.ToLowerInvariant() switch
        {
            "on" => true,
            "off" => false,
            "yes" => true,
            "no" => false,
            _ => defaultValue,
        };
    }

    private static IReadOnlyList<PresetHotkeyBinding> ParsePresetHotkeys(IEnumerable<string> entries)
    {
        var parsed = new List<PresetHotkeyBinding>();
        foreach (var entry in entries)
        {
            var separator = entry.IndexOf('|');
            if (separator <= 0 || separator == entry.Length - 1)
            {
                continue;
            }

            var hotkeyText = entry[..separator].Trim();
            var presetPath = entry[(separator + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(presetPath))
            {
                continue;
            }

            var hotkey = HotkeyBindingSerializer.ParseOrDefault(hotkeyText, new HotkeyBinding());
            parsed.Add(new PresetHotkeyBinding
            {
                Hotkey = hotkey,
                PresetPath = presetPath,
            });
        }

        return parsed;
    }
}
