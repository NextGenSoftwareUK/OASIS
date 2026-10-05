using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.Enums;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;

/// <summary>
/// Interface for Radix blockchain service operations
/// </summary>
public interface IRadixService : IOASISBridge
{
    /// <summary>
    /// Gets the Radix address from a public key
    /// </summary>
    OASISResult<string> GetAddress(PublicKey publicKey, RadixAddressType addressType, 
        RadixNetworkType networkType, CancellationToken token = default);
}

