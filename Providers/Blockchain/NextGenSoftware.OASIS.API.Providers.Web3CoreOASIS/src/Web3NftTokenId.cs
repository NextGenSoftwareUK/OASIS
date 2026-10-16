using System.Globalization;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

internal static class Web3NftTokenId
{
    internal static bool TryParse(string value, out BigInteger tokenId)
    {
        return BigInteger.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out tokenId)
            && tokenId >= BigInteger.Zero && tokenId < (BigInteger.One << 256);
    }
}
