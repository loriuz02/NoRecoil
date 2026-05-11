namespace NoRecoil.Core;

internal static class NumericValueNormalizer
{
    public static int ToAbsolute(int value) => value == int.MinValue ? int.MaxValue : Math.Abs(value);
}
