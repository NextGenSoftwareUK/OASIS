using System;
using System.Globalization;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

internal static class NearNativeAmount
{
    private static readonly BigInteger Maximum = (BigInteger.One << 128) - 1;

    public static string ToYocto(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var bits = decimal.GetBits(amount);
        var units = new BigInteger((uint)bits[0]) + (new BigInteger((uint)bits[1]) << 32)
            + (new BigInteger((uint)bits[2]) << 64);
        var scale = (bits[3] >> 16) & 255;
        if (scale <= 24) units *= BigInteger.Pow(10, 24 - scale);
        else
        {
            units = BigInteger.DivRem(units, BigInteger.Pow(10, scale - 24), out var remainder);
            if (remainder != 0) throw new ArgumentException("NEAR requires exact yoctoNEAR units.", nameof(amount));
        }
        if (units > Maximum) throw new ArgumentOutOfRangeException(nameof(amount), "NEAR balance exceeds uint128.");
        return units.ToString(CultureInfo.InvariantCulture);
    }

    public static decimal FromYocto(string value)
    {
        var units = BigInteger.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
        if (units < 0 || units > Maximum) throw new ArgumentOutOfRangeException(nameof(value));
        // Scale before decimal conversion: a valid uint128 balance can exceed decimal.MaxValue in raw units.
        // The decimal API necessarily rounds fractions beyond its available significant digits.
        var digits = units.ToString(CultureInfo.InvariantCulture).PadLeft(25, '0');
        return decimal.Parse(digits.Insert(digits.Length - 24, "."), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    }
}
