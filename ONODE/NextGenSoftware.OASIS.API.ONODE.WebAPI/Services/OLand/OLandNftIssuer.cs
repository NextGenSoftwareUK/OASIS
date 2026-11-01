using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;

public sealed class OLandNftIssuer : IOLandNftIssuer
{
    public async Task<(string TransactionId, string Message)> Mint(OLandOrder order, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (order.State != "paid" || order.PaidUtc == null || string.IsNullOrWhiteSpace(order.PaymentIdentity))
            throw new OLandCommerceException("OLAND_PAYMENT_REQUIRED", "Only a verified paid order can issue land NFTs.");
        var result = await new NFTManager(Guid.Parse(order.AvatarId)).MintNftAsync(new MintWeb4NFTRequest
        {
            NumberToMint = order.ParcelIds.Count,
            Title = "OLAND " + order.Id, Description = order.ParcelIds.Count + " parcels, each1x1 metre.", Symbol = "OLAND",
            MintedByAvatarId = Guid.Parse(order.AvatarId), SendToAddressAfterMinting = order.ReceivingWallet,
            OnChainProvider = new EnumValue<ProviderType>(Enum.Parse<ProviderType>(order.ProviderType)),
            OffChainProvider = new EnumValue<ProviderType>(ProviderManager.Instance.CurrentStorageProviderType.Value),
            MemoText = "oland:" + order.Id,
            MetaData = new Dictionary<string, string> { { "olandOrderId", order.Id }, { "olandParcelIds", JsonSerializer.Serialize(order.ParcelIds) },
                { "olandParcelAreaSquareMetres", "1" }, { "olandPaymentIdentity", order.PaymentIdentity } }
        });
        string transaction = result.Result?.Web3NFTs?.FirstOrDefault()?.MintTransactionHash;
        if (result.IsError || string.IsNullOrWhiteSpace(transaction))
            return (null, "Land ownership is paid; NFT delivery needs reconciliation. " + result.Message);
        return (transaction, "OLAND NFTs issued for the paid parcel order.");
    }
}
