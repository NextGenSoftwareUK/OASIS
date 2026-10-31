using System;
using System.Numerics;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public static class ExactEvmTokenAmount
{
    public static BigInteger ToUnits(decimal amount,byte decimals)
    {
        if(amount<=0) throw new ArgumentOutOfRangeException(nameof(amount));
        var bits=decimal.GetBits(amount);var scale=(bits[3]>>16)&255;
        var coefficient=new BigInteger((uint)bits[0])+(new BigInteger((uint)bits[1])<<32)+(new BigInteger((uint)bits[2])<<64);
        BigInteger units;
        if(decimals>=scale) units=coefficient*BigInteger.Pow(10,decimals-scale);
        else
        {
            units=BigInteger.DivRem(coefficient,BigInteger.Pow(10,scale-decimals),out var remainder);
            if(remainder!=0) throw new ArgumentException("Token amount exceeds the contract's decimal precision.",nameof(amount));
        }
        if(units<=0 || units>=(BigInteger.One<<256)) throw new ArgumentOutOfRangeException(nameof(amount),"Token amount exceeds uint256.");
        return units;
    }
}
