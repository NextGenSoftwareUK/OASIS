using System;
using System.Collections.Generic;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.TenderlyOASIS;

/// <summary>Anonymous finalized account observations from Tenderly's official Polygon public RPC.</summary>
public sealed class TenderlyPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "TenderlyPolygonAccountAddress";
    private static readonly Uri Origin = new("https://tenderly.rpc.polygon.community/");
    private static readonly Network Mainnet = new(PType.TenderlyOASIS, "TenderlyPolygon", "0x89",
        "0xa9c28ce2141b56c474f1dc504bee9b01eb1bd7d1a507580d5519d4437a97de1b", AddressMetadata);

    public TenderlyPublicAccountMetadataSource(IEnumerable<string> addresses, Guid owner)
        : this(CreateClient(), addresses, owner, ownsClient: true) { }

    internal TenderlyPublicAccountMetadataSource(HttpClient client, IEnumerable<string> addresses, Guid owner, bool ownsClient = false)
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
