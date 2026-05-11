namespace NoRecoil.Core;

internal static class NumericValueNormalizer
{
    /// <summary>
    /// Converts an integer to an absolute value and safely maps <see cref="int.MinValue"/> to <see cref="int.MaxValue"/>,
    /// because <c>Math.Abs(int.MinValue)</c> cannot be represented in a signed 32-bit integer.
    /// </summary>
    public static int ToAbsolute(int value) => value == int.MinValue ? int.MaxValue : Math.Abs(value);
}
