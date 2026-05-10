using System.Globalization;
using System.Text;

namespace NoRecoil.Core;

public static class PresetIniSerializer
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public static RecoilPreset Parse(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var values = new Dictionary<string, int>(KeyComparer)
        {
            ["Right"] = 0,
            ["Down"] = 0,
            ["Left"] = 0,
            ["Up"] = 0,
            ["Delay"] = 0,
        };

        foreach (var rawLine in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawLine.StartsWith(';') || rawLine.StartsWith('#') || rawLine.StartsWith('['))
            {
                continue;
            }

            var separator = rawLine.IndexOf('=');
            if (separator <= 0 || separator == rawLine.Length - 1)
            {
                continue;
            }

            var key = rawLine[..separator].Trim();
            if (!values.ContainsKey(key))
            {
                continue;
            }

            var valueText = rawLine[(separator + 1)..].Trim();
            if (int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                values[key] = value;
            }
        }

        return new RecoilPreset
        {
            Right = values["Right"],
            Down = values["Down"],
            Left = values["Left"],
            Up = values["Up"],
            Delay = values["Delay"],
        };
    }

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

    public static RecoilPreset LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Parse(File.ReadAllText(path));
    }

    public static void SaveToFile(string path, RecoilPreset preset, bool includeSectionHeader = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(preset);

        File.WriteAllText(path, Serialize(preset, includeSectionHeader));
    }
}
