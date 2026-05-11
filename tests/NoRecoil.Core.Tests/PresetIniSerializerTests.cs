using NoRecoil.Core;

namespace NoRecoil.Core.Tests;

public class PresetIniSerializerTests
{
    [Fact]
    public void Parse_ReadsRecoilSectionFormat()
    {
        const string input = """
            [Recoil]
            Right=0
            Down=37
            Left=0
            Up=0
            Delay=0
            """;

        var preset = PresetIniSerializer.Parse(input);

        Assert.Equal(0, preset.Right);
        Assert.Equal(37, preset.Down);
        Assert.Equal(0, preset.Left);
        Assert.Equal(0, preset.Up);
        Assert.Equal(0, preset.Delay);
    }

    [Fact]
    public void Parse_ReadsLegacyFormatWithoutSection()
    {
        const string input = """
            Right=7
            Down=14
            Left=2
            Up=1
            Delay=3
            """;

        var preset = PresetIniSerializer.Parse(input);

        Assert.Equal(7, preset.Right);
        Assert.Equal(14, preset.Down);
        Assert.Equal(2, preset.Left);
        Assert.Equal(1, preset.Up);
        Assert.Equal(3, preset.Delay);
    }

    [Fact]
    public void Parse_ReadsAliasAndColonBasedKeys()
    {
        const string input = """
            ﻿R:9
            DownValue=14
            L=2
            U=1
            Delay=3
            """;

        var preset = PresetIniSerializer.Parse(input);

        Assert.Equal(9, preset.Right);
        Assert.Equal(14, preset.Down);
        Assert.Equal(2, preset.Left);
        Assert.Equal(1, preset.Up);
        Assert.Equal(3, preset.Delay);
    }

    [Fact]
    public void Parse_NormalizesDirectionalValuesToAbsolute()
    {
        const string input = """
            Right=-7
            Down=-14
            Left=-2
            Up=-1
            Delay=-3
            """;

        var preset = PresetIniSerializer.Parse(input);

        Assert.Equal(7, preset.Right);
        Assert.Equal(14, preset.Down);
        Assert.Equal(2, preset.Left);
        Assert.Equal(1, preset.Up);
        Assert.Equal(-3, preset.Delay);
    }

    [Fact]
    public void Serialize_WritesRecoilSectionWhenEnabled()
    {
        var preset = new RecoilPreset
        {
            Right = 5,
            Down = 20,
            Left = 1,
            Up = 0,
            Delay = 2,
        };

        var ini = PresetIniSerializer.Serialize(preset, includeSectionHeader: true);

        Assert.Contains("[Recoil]", ini);
        Assert.Contains("Right=5", ini);
        Assert.Contains("Down=20", ini);
        Assert.Contains("Left=1", ini);
        Assert.Contains("Up=0", ini);
        Assert.Contains("Delay=2", ini);
    }
}
