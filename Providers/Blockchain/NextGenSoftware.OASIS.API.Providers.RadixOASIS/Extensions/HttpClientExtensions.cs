using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;

/// <summary>
/// Extensions for HttpClient to interact with Radix API
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Gets construction metadata (current epoch) from Radix network
    /// </summary>
    public static async Task<ConstructionMetadataResponse?> GetConstructionMetadataAsync(
        this HttpClient httpClient,
        RadixOASISConfig config)
    {
        var network = config.NetworkId == 1 ? RadixBridgeHelper.MainNet : RadixBridgeHelper.StokeNet;
        
        var data = new { network };
        
        var result = await HttpClientHelper.PostAsync<object, ConstructionMetadataResponse>(
            httpClient,
            $"{config.HostUri}/core/status/network-configuration",
            data);

        return result.Result;
    }
}

