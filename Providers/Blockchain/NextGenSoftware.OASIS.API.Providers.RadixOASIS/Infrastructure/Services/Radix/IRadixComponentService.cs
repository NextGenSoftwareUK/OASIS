using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;

/// <summary>
/// Interface for Radix component interaction operations (OASIS Storage component)
/// </summary>
public interface IRadixComponentService
{
    /// <summary>
    /// Calls a component method (read-only, no transaction required)
    /// </summary>
    Task<OASISResult<string>> CallComponentMethodAsync(
        string componentAddress,
        string methodName,
        List<object> args,
        CancellationToken token = default);

    /// <summary>
    /// Calls a component method that modifies state (requires transaction)
    /// </summary>
    Task<OASISResult<string>> CallComponentMethodTransactionAsync(
        string componentAddress,
        string methodName,
        List<object> args,
        string senderPrivateKey,
        CancellationToken token = default);
}

