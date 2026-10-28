using NoRecoil.Core;

namespace NoRecoil.Core.Tests;

public class GamepadStickPositionTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(100, 0, 0, 0, 32767, 0)]
    [InlineData(0, 100, 0, 0, -32768, 0)]
    [InlineData(0, 0, 100, 0, 0, -32768)]
    [InlineData(0, 0, 0, 100, 0, 32767)]
    [InlineData(60, 10, 70, 20, 16383, -16384)]
    [InlineData(50, 50, 30, 30, 0, 0)]
    [InlineData(200, 0, 200, 0, 32767, -32768)]
    [InlineData(0, 200, 0, 200, -32768, 32767)]
    [InlineData(int.MaxValue, int.MinValue, int.MaxValue, int.MinValue, 32767, -32768)]
    [InlineData(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue, -32768, 32767)]
    public void FromPreset_MapsDirectionsAndClampsAxes(
        int right, int left, int down, int up, int expectedX, int expectedY)
    {
        var position = GamepadStickPosition.FromPreset(new RecoilPreset
        {
            Right = right,
            Left = left,
            Down = down,
            Up = up,
        });

        Assert.Equal(expectedX, position.X);
        Assert.Equal(expectedY, position.Y);
    }

    [Fact]
    public void FromPreset_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => GamepadStickPosition.FromPreset(null!));
    }
}
