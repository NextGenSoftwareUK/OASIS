using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.CrossChain
{
    public enum CrossChainTransferStatus
    {
        Unknown,
        Pending,
        Completed,
        Failed,
        Refunded
    }

    /// <summary>A transfer or message tracked by a cross-chain protocol, normalised across bridges.</summary>
    public sealed record CrossChainTransfer
    {
        /// <summary>The protocol's own identifier (Wormhole VAA id, Axelar GMP id, LayerZero GUID, deBridge order id, ...).</summary>
        public string Id { get; init; }
        public string Protocol { get; init; }
        public CrossChainTransferStatus Status { get; init; }
        /// <summary>The status exactly as the protocol reported it.</summary>
        public string RawStatus { get; init; }
        public string SourceChain { get; init; }
        public string SourceTxHash { get; init; }
        public string DestinationChain { get; init; }
        public string DestinationTxHash { get; init; }
        public string Sender { get; init; }
        public string Recipient { get; init; }
        public string Token { get; init; }
        public string Amount { get; init; }
        public DateTimeOffset? Timestamp { get; init; }
    }

    /// <summary>Implemented by cross-chain protocol providers to look up transfers from the protocol's own indexer/API.</summary>
    public interface ICrossChainTracker
    {
        Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersBySourceTxAsync(string sourceTxHash, CancellationToken cancellationToken = default);

        Task<OASISResult<IReadOnlyList<CrossChainTransfer>>> GetTransfersByAddressAsync(string address, int limit = 50, CancellationToken cancellationToken = default);
    }
}
