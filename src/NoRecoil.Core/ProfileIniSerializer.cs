using System.Globalization;
using System.Text;

namespace NoRecoil.Core;

public static class ProfileIniSerializer
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;
    private const string EnableRecoilActionTarget = "__action__:EnableRecoil";
    private const string DisableRecoilActionTarget = "__action__:DisableRecoil";

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
            ["OutputBackend"] = nameof(RecoilOutputBackend.Mouse),
            ["BluetoothPort"] = string.Empty,
            ["EnableHotkey"] = string.Empty,
            ["DisableHotkey"] = string.Empty,
        };
        var hotkeyEntries = new List<string>();

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
            if (key.StartsWith("Hotkey", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("PresetHotkey", StringComparison.OrdinalIgnoreCase))
            {
                var presetValueText = separator == rawLine.Length - 1 ? string.Empty : rawLine[(separator + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(presetValueText))
                {
                    hotkeyEntries.Add(presetValueText);
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

        var parsedHotkeys = ParseHotkeys(hotkeyEntries);
        AppendLegacyActionHotkey(parsedHotkeys, values["EnableHotkey"], EnableRecoilActionTarget, ProfileSettings.DefaultEnableHotkeyKey);
        AppendLegacyActionHotkey(parsedHotkeys, values["DisableHotkey"], DisableRecoilActionTarget, ProfileSettings.DefaultDisableHotkeyKey);

        return new ProfileSettings
        {
            Preset = new RecoilPreset
            {
                Right = ParseAndNormalizeIntOrDefault(values["Right"]),
                Down = ParseAndNormalizeIntOrDefault(values["Down"]),
                Left = ParseAndNormalizeIntOrDefault(values["Left"]),
                Up = ParseAndNormalizeIntOrDefault(values["Up"]),
                Delay = ParseIntOrDefault(values["Delay"]),
            },
            RecoilEnabled = ParseBoolOrDefault(values["RecoilEnabled"], defaultValue: false),
            RequireRightMouseButton = ParseBoolOrDefault(values["RequireRightMouseButton"], defaultValue: true),
            OutputBackend = ParseOutputBackend(values["OutputBackend"]),
            BluetoothPort = values["BluetoothPort"],
            Hotkeys = parsedHotkeys,
        };
    }

    public static string Serialize(ProfileSettings settings, bool includeSectionHeader = true)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!Enum.IsDefined(settings.OutputBackend))
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Unknown recoil output backend.");
        }

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
        output.AppendLine($"OutputBackend={settings.OutputBackend}");
        output.AppendLine($"BluetoothPort={settings.BluetoothPort}");

        var maxHotkeys = Math.Min(settings.Hotkeys.Count, ProfileSettings.MaxHotkeys);
        for (var i = 0; i < maxHotkeys; i++)
        {
            var entry = settings.Hotkeys[i];
            if (string.IsNullOrWhiteSpace(entry.PresetPath))
            {
                continue;
            }

            var hotkey = HotkeyBindingSerializer.Serialize(entry.Hotkey);
            output.AppendLine($"Hotkey{i + 1}={hotkey}|{entry.PresetPath}");
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

    private static RecoilOutputBackend ParseOutputBackend(string valueText)
    {
        if (Enum.TryParse<RecoilOutputBackend>(valueText, ignoreCase: true, out var backend)
            && Enum.IsDefined(backend))
        {
            return backend;
        }

        throw new FormatException($"Unknown recoil output backend: '{valueText}'.");
    }

    private static int ParseAndNormalizeIntOrDefault(string valueText)
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

    private static List<PresetHotkeyBinding> ParseHotkeys(IEnumerable<string> entries)
    {
        var parsed = new List<PresetHotkeyBinding>();
        foreach (var entry in entries)
        {
            if (parsed.Count >= ProfileSettings.MaxHotkeys)
            {
                break;
            }

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

    private static void AppendLegacyActionHotkey(List<PresetHotkeyBinding> target, string text, string actionTarget, int fallbackKey)
    {
        if (string.IsNullOrWhiteSpace(text) || target.Count >= ProfileSettings.MaxHotkeys)
        {
            return;
        }

        var parsedHotkey = HotkeyBindingSerializer.ParseOrDefault(text, new HotkeyBinding
        {
            Key = fallbackKey,
            Modifiers = HotkeyModifiers.None,
        });

        if (target.Any(existing => existing.Hotkey.Key == parsedHotkey.Key
                                   && existing.Hotkey.Modifiers == parsedHotkey.Modifiers))
        {
            return;
        }

        target.Add(new PresetHotkeyBinding
        {
            Hotkey = parsedHotkey,
            PresetPath = actionTarget,
        });
    }
}
