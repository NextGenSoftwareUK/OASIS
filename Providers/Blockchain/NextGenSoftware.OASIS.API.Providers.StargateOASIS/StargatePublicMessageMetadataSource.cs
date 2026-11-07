using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Providers.LayerZeroV2OASIS;

namespace NextGenSoftware.OASIS.API.Providers.StargateOASIS;

/// <summary>Bounded Scan observations for the Stargate Ethereum TokenMessaging receiver.</summary>
public sealed class StargatePublicMessageMetadataSource : LayerZeroPublicMessageMetadataSource
{
    public new const string MessageIdMetadata = "StargateMessageId";
    private const uint EthereumEndpoint = 30101;
    private const string TokenMessaging = "0x6d6620efa72948c5f68a3c8646d58c00d3f4a980";

    public StargatePublicMessageMetadataSource(HttpClient client, IEnumerable<string> messageIds,
        Guid sourceOwner, bool ownsClient = false)
        : base(client, messageIds, sourceOwner, ownsClient, PType.StargateOASIS,
            MessageIdMetadata, "stargate-scan-messages", ValidateReceiver) { }

    public new static StargatePublicMessageMetadataSource FromJson(string json)
        => ReadConfiguration(json, (client, keys, owner) => new StargatePublicMessageMetadataSource(client, keys, owner, true));

    private static void ValidateReceiver(LayerZeroScanMessageClient.Observation message)
    {
        if (message.DestinationEndpoint != EthereumEndpoint ||
            !string.Equals(message.DestinationAddress, TokenMessaging, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Scan returned a message outside the Stargate Ethereum TokenMessaging source.");
    }
}
