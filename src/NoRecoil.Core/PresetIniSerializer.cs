using System.Globalization;
using System.Text;

namespace NoRecoil.Core;

public static class PresetIniSerializer
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly IReadOnlyDictionary<string, string> KeyAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["R"] = "Right",
        ["L"] = "Left",
        ["D"] = "Down",
        ["U"] = "Up",
        ["RightValue"] = "Right",
        ["LeftValue"] = "Left",
        ["DownValue"] = "Down",
        ["UpValue"] = "Up",
    };

    public static RecoilPreset Parse(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var values = CreateDefaultValueMap();

        foreach (var rawLine in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawLine.StartsWith(';') || rawLine.StartsWith('#') || rawLine.StartsWith('['))
            {
                continue;
            }

            ApplyLine(values, rawLine);
        }

        return BuildPreset(values);
    }

    public static RecoilPresetFile ParseFile(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var primary = CreateDefaultValueMap();
        var secondary = CreateDefaultValueMap();
        var inSecondary = false;
        var hasSecondary = false;

        foreach (var rawLine in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawLine.StartsWith(';') || rawLine.StartsWith('#'))
            {
                continue;
            }

            if (rawLine.StartsWith('['))
            {
                var header = rawLine.Trim('[', ']').Trim();
                if (header.Equals("Secondary", StringComparison.OrdinalIgnoreCase))
                {
                    inSecondary = true;
                    hasSecondary = true;
                }
                else
                {
                    inSecondary = false;
                }

                continue;
            }

            ApplyLine(inSecondary ? secondary : primary, rawLine);
        }

        return new RecoilPresetFile(BuildPreset(primary), hasSecondary ? BuildPreset(secondary) : null);
    }

    private static Dictionary<string, int> CreateDefaultValueMap() => new(KeyComparer)
    {
        ["Right"] = 0,
        ["Down"] = 0,
        ["Left"] = 0,
        ["Up"] = 0,
        ["Delay"] = 0,
    };

    private static void ApplyLine(IDictionary<string, int> values, string rawLine)
    {
        var separator = rawLine.IndexOf('=');
        if (separator <= 0)
        {
            separator = rawLine.IndexOf(':');
        }

        if (separator <= 0 || separator == rawLine.Length - 1)
        {
            return;
        }

        var key = ResolveKey(rawLine[..separator].Trim());
        if (key is null || !values.ContainsKey(key))
        {
            return;
        }

        var valueText = rawLine[(separator + 1)..].Trim();
        if (int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            values[key] = key.Equals("Delay", StringComparison.OrdinalIgnoreCase)
                ? value
                : NumericValueNormalizer.ToAbsolute(value);
        }
    }

    private static RecoilPreset BuildPreset(IReadOnlyDictionary<string, int> values) => new()
    {
        Right = values["Right"],
        Down = values["Down"],
        Left = values["Left"],
        Up = values["Up"],
        Delay = values["Delay"],
    };

    public static string Serialize(RecoilPreset preset, bool includeSectionHeader = true)
    {
        ArgumentNullException.ThrowIfNull(preset);

        var output = new StringBuilder();

        if (includeSectionHeader)
        {
            output.AppendLine("[Recoil]");
        }

        output.AppendLine($"Right={preset.Right}");
        output.AppendLine($"Down={preset.Down}");
        output.AppendLine($"Left={preset.Left}");
        output.AppendLine($"Up={preset.Up}");
        output.AppendLine($"Delay={preset.Delay}");

        return output.ToString();
    }

    public static string Serialize(RecoilPresetFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var output = new StringBuilder();
        output.Append(Serialize(file.Primary, includeSectionHeader: true));

        if (file.Secondary is not null)
        {
            output.AppendLine("[Secondary]");
            output.Append(Serialize(file.Secondary, includeSectionHeader: false));
        }

        return output.ToString();
    }

    public static RecoilPreset LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Parse(File.ReadAllText(path));
    }

    public static RecoilPresetFile LoadFileFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return ParseFile(File.ReadAllText(path));
    }

    public static void SaveToFile(string path, RecoilPreset preset, bool includeSectionHeader = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(preset);

        File.WriteAllText(path, Serialize(preset, includeSectionHeader));
    }

    public static void SaveToFile(string path, RecoilPresetFile file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(file);

        File.WriteAllText(path, Serialize(file));
    }

    private static string? ResolveKey(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return null;
        }

        var key = rawKey.TrimStart('\uFEFF'); // Remove UTF BOM when present at file start.
        if (KeyAliases.TryGetValue(key, out var alias))
        {
            return alias;
        }

        return key;
    }
}
