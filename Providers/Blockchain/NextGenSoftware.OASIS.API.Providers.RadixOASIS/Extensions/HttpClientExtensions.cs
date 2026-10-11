using System.Net.Http;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;

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
        // HostUri is the Radix Gateway API, which has no /core/* routes; the current epoch is in /status/gateway-status.
        // Previous Core API call, kept for reference:
        //var network = config.NetworkId == 1 ? RadixBridgeHelper.MainNet : RadixBridgeHelper.StokeNet;
        //var result = await HttpClientHelper.PostAsync<object, ConstructionMetadataResponse>(
        //    httpClient, $"{config.HostUri}/core/status/network-configuration", new { network });
        //return result.Result;
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync($"{config.HostUri.TrimEnd('/')}/status/gateway-status", content);
        if (!response.IsSuccessStatusCode) return null;
        var status = System.Text.Json.Nodes.JsonNode.Parse(await response.Content.ReadAsStringAsync());
        var epoch = (ulong?)status?["ledger_state"]?["epoch"];
        return epoch == null ? null : new ConstructionMetadataResponse { CurrentEpoch = epoch.Value };
    }
}

