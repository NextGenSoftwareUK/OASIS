using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

/// <summary>A confirmed mint must not be repeated because subsequent processing or delivery failed.</summary>
public sealed class HederaNftPostMintReconciliationException : InvalidOperationException, INativeMutationReconciliationRequired
{
    public string ConfirmedMintTransactionId { get; }
    public string RequestedDeliveryTransactionId { get; }
    public string OriginalTransactionId => RequestedDeliveryTransactionId ?? ConfirmedMintTransactionId;

    internal HederaNftPostMintReconciliationException(string mintId,string deliveryId,Exception cause)
        : base("Hedera NFT mint confirmed as "+mintId+" but post-mint processing/delivery failed; reconcile original transactions before retrying.",cause)
    {
        if(string.IsNullOrWhiteSpace(mintId))throw new ArgumentException("Confirmed mint identity required.",nameof(mintId));
        if(deliveryId!=null&&string.IsNullOrWhiteSpace(deliveryId))throw new ArgumentException("Delivery identity must be explicit or absent.",nameof(deliveryId));
        ArgumentNullException.ThrowIfNull(cause);
        ConfirmedMintTransactionId=mintId;RequestedDeliveryTransactionId=deliveryId;
    }
}
