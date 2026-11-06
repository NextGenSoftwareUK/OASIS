using System;
using System.Collections.Generic;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.QuickNodeOASIS;

/// <summary>Anonymous finalized account observations from QuickNode's official Polygon public RPC.</summary>
public sealed class QuickNodePublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "QuickNodePolygonAccountAddress";
    private static readonly Uri Origin = new("https://rpc-mainnet.matic.quiknode.pro/");
    private static readonly Network Mainnet = new(PType.QuickNodeOASIS, "QuickNodePolygon", "0x89",
        "0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b", AddressMetadata);

    public QuickNodePublicAccountMetadataSource(IEnumerable<string> addresses, Guid owner)
        : this(CreateClient(), addresses, owner, ownsClient: true) { }

    internal QuickNodePublicAccountMetadataSource(HttpClient client, IEnumerable<string> addresses, Guid owner, bool ownsClient = false)
        : base(client, Origin, Mainnet.ChainId, addresses, owner, ownsClient, Mainnet) { }

    private static HttpClient CreateClient()
    {
        var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        transport.UseCookies = false;
        transport.UseProxy = false;
        var client = new HttpClient(transport) { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("OASISAPI/1.0");
        return client;
    }
}
