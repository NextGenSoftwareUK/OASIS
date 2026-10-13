using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using ChainProvider = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS
{
    private async Task<OASISResult<IWeb3NFTTransactionResponse>> MintHederaNftAsync(IMintWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        string confirmedMintId=null;
        string requestedDeliveryId=null;
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var tokenId = request.CollectionPublicKey;
            if (!System.Text.RegularExpressions.Regex.IsMatch(tokenId ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                throw new ArgumentException("CollectionPublicKey must identify an existing HTS NFT token.");
            if (request.OnChainProvider.HasValue && request.OnChainProvider.Value != ChainProvider.HashgraphOASIS)
                throw new ArgumentException("The requested on-chain provider must be HashgraphOASIS.");
            if (request.NFTStandardType.HasValue && request.NFTStandardType.Value != Core.Enums.NFTStandardType.HederaHTS)
                throw new ArgumentException("Native HTS NFT minting requires HederaHTS, not another chain's standard.");
            if (request.FreezeMetadata == true || request.RoyaltyPercentage.GetValueOrDefault() != 0)
                throw new ArgumentException("Metadata authority and royalty changes require an HTS collection operation, not an NFT mint.");
            if (request.Price < 0 || request.Discount < 0)
                throw new ArgumentException("NFT price and discount must be nonnegative.");
            if (request.SaleStartDate.HasValue && request.SaleEndDate.HasValue && request.SaleStartDate > request.SaleEndDate)
                throw new ArgumentException("NFT sale start must not follow sale end.");
            var count = request.NumberToMint ?? 1;
            if (count < 1 || count > 10)
                throw new ArgumentException("This HTS mint transaction supports 1 to 10 NFT serials; larger batches require separate transactions.");
            if (request.Amount != 0 && request.Amount != count)
                throw new ArgumentException("Amount must agree with NumberToMint when supplied for an NFT mint.");
            var metadata = request.StoreNFTMetaDataOnChain == true ? request.JSONMetaData : request.JSONMetaDataURL;
            if (string.IsNullOrWhiteSpace(metadata) || CollectionUtf8.GetByteCount(metadata) > 100)
                throw new ArgumentException("HTS NFT metadata requires 1 to 100 UTF-8 bytes; supply a metadata URL or explicit short on-chain JSON.");
            if (request.StoreNFTMetaDataOnChain == true)
                using (System.Text.Json.JsonDocument.Parse(metadata)) { }
            if (request.MintedByAvatarId == Guid.Empty)
                throw new ArgumentException("The minting avatar's real Hedera wallet is required.");
            // Resolve every supplied selector before minting; conflicting destinations must not mint first.
            var targets = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.SendToAddressAfterMinting)) targets.Add(request.SendToAddressAfterMinting);
            if (request.SendToAvatarAfterMintingId != Guid.Empty)
            {
                var target = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.SendToAvatarAfterMintingId, ChainProvider.HashgraphOASIS);
                if (target.IsError || target.Result == null) throw new InvalidOperationException("Recipient avatar wallet lookup failed.");
                targets.Add(target.Result.WalletAddress);
            }
            if (!string.IsNullOrWhiteSpace(request.SendToAvatarAfterMintingUsername))
            {
                var target = await WalletManager.GetAvatarDefaultWalletByUsernameAsync(request.SendToAvatarAfterMintingUsername, providerType: ChainProvider.HashgraphOASIS);
                if (target.IsError || target.Result == null) throw new InvalidOperationException("Recipient username wallet lookup failed.");
                targets.Add(target.Result.WalletAddress);
            }
            if (!string.IsNullOrWhiteSpace(request.SendToAvatarAfterMintingEmail))
            {
                var target = await WalletManager.GetAvatarDefaultWalletByEmailAsync(request.SendToAvatarAfterMintingEmail, ChainProvider.HashgraphOASIS);
                if (target.IsError || target.Result == null) throw new InvalidOperationException("Recipient email wallet lookup failed.");
                targets.Add(target.Result.WalletAddress);
            }
            if (targets.Any(target => !System.Text.RegularExpressions.Regex.IsMatch(target ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                || targets.Distinct(StringComparer.Ordinal).Count() > 1)
                throw new ArgumentException("All recipient selectors must resolve to the same numeric Hedera account.");
            var walletResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.MintedByAvatarId, ChainProvider.HashgraphOASIS, showPrivateKeys: true);
            if (walletResult.IsError || walletResult.Result == null) throw new InvalidOperationException("Minting wallet lookup failed.");
            var wallet = walletResult.Result;
            var requestedMintId = HederaSdkClient.CreateTransactionId(wallet.WalletAddress);
            var committed = await HederaSdkClient.InvokeAsync("nftMint", new
            {
                network = _hederaNetwork, nodes = _consensusNodes, sender = wallet.WalletAddress, recipient = wallet.WalletAddress,
                tokenId, metadata = Enumerable.Repeat(metadata, count).ToArray(), privateKey = wallet.PrivateKey,
                publicKey = wallet.PublicKey, keyType = "der", memo = request.MemoText,
                transactionId = requestedMintId
            });
            result.Result = new Web3NFTTransactionResponse();
            var serials = ApplyHederaMintReceipt(result.Result, committed, requestedMintId, count);
            var mintId = result.Result.TransactionResult;
            confirmedMintId=mintId;
            foreach (var serial in serials) ParseHederaNftAddress(tokenId + "/" + serial);
            var nft = new Web3NFT
            {
                NFTTokenAddress = tokenId + "/" + serials[0], CollectionPublicKey = tokenId, MintTransactionHash = mintId,
                NFTStandardType = new EnumValue<Core.Enums.NFTStandardType>(Core.Enums.NFTStandardType.HederaHTS),
                NFTMintedUsingWalletAddress = wallet.WalletAddress, MintedByAvatarId = request.MintedByAvatarId,
                Title = request.Title, Description = request.Description, JSONMetaData = request.JSONMetaData,
                JSONMetaDataURL = request.JSONMetaDataURL, Image = request.Image, ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail, ThumbnailUrl = request.ThumbnailUrl, Tags = request.Tags,
                Symbol = request.Symbol, MemoText = request.MemoText, MintedOn = DateTime.UtcNow,
                Price = request.Price.GetValueOrDefault(), Discount = request.Discount.GetValueOrDefault(),
                IsForSale = request.IsForSale.GetValueOrDefault(), SaleStartDate = request.SaleStartDate, SaleEndDate = request.SaleEndDate,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain == true,
                OffChainProvider = request.OffChainProvider.HasValue ? new EnumValue<ChainProvider>(request.OffChainProvider.Value) : null,
                NFTOffChainMetaType = request.NFTOffChainMetaType.HasValue ? new EnumValue<NFTOffChainMetaType>(request.NFTOffChainMetaType.Value) : null,
                OnChainProvider = new EnumValue<ChainProvider>(ChainProvider.HashgraphOASIS),
                MetaData = request.MetaData == null ? new() : new(request.MetaData)
            };
            nft.MetaData["HederaSerials"] = System.Text.Json.JsonSerializer.Serialize(serials);
            result.Result = CreateHederaMintResponse(nft, serials, mintId);
            var recipient = targets.FirstOrDefault();
            if (recipient != null && recipient != wallet.WalletAddress)
            {
                requestedDeliveryId=HederaSdkClient.CreateTransactionId(wallet.WalletAddress);
                var sent = await HederaSdkClient.InvokeAsync("nftTransfer", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, sender = wallet.WalletAddress, recipient,
                    tokenId, serials, privateKey = wallet.PrivateKey, publicKey = wallet.PublicKey, keyType = "der", memo = request.MemoText,
                    transactionId = requestedDeliveryId
                });
                if (sent.GetProperty("status").GetString() != "SUCCESS") throw new InvalidOperationException("Mint completed but recipient transfer has no successful receipt.");
                RecordHederaNftDelivery(result.Result, sent.GetProperty("transactionId").GetString(), recipient);
            }
            result.Message = "HTS NFT mint and requested recipient transfer confirmed by consensus receipts.";
        }
        catch (Exception ex)
        {
            ex=RetainHederaPostMintEvidence(ex,confirmedMintId,requestedDeliveryId);
            OASISErrorHandling.HandleError(ref result, $"Hedera NFT mint failed: {ex.Message}", ex);
        }
        return result;
    }

    private static Exception RetainHederaPostMintEvidence(Exception cause,string confirmedMintId,string requestedDeliveryId)
        => confirmedMintId==null?cause:new HederaNftPostMintReconciliationException(confirmedMintId,requestedDeliveryId,cause);

    private static Web3NFTTransactionResponse CreateHederaMintResponse(Web3NFT template, string[] serials, string mintId)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (serials == null || serials.Length == 0 || serials.Distinct(StringComparer.Ordinal).Count() != serials.Length)
            throw new InvalidOperationException("A minted batch requires distinct original serials.");
        var nfts = serials.Select(serial =>
        {
            var address = template.CollectionPublicKey + "/" + serial;
            ParseHederaNftAddress(address);
            var nft = OasisJson.Deserialize<Web3NFT>(OasisJson.Serialize(template));
            nft.NFTTokenAddress = address;
            nft.Id = CreateDeterministicGuid($"{ChainProvider.HashgraphOASIS}:nft:{address}");
            nft.MintTransactionHash = mintId;
            return (IWeb3NFT)nft;
        }).ToList();
        return new Web3NFTTransactionResponse { TransactionResult = mintId, Web3NFT = nfts[0], Web3NFTs = nfts };
    }

    private static string[] ApplyHederaMintReceipt(IWeb3NFTTransactionResponse response,
        System.Text.Json.JsonElement receipt, string requestedId, int count)
    {
        var verifiedSuccess=false;
        try
        {
        if (receipt.GetProperty("status").GetString() != "SUCCESS"
            || receipt.GetProperty("transactionId").GetString() != requestedId)
            throw new InvalidOperationException("NFT mint receipt identity/status mismatch; reconcile the original transaction " + requestedId);
        verifiedSuccess=true;
        response.TransactionResult = requestedId;
        var serials = receipt.GetProperty("serials").EnumerateArray().Select(value => value.GetString()).ToArray();
        if (serials.Length != count || serials.Distinct(StringComparer.Ordinal).Count() != count
            || serials.Any(value => !long.TryParse(value, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var serial) || serial <= 0))
            throw new InvalidOperationException("Mint " + requestedId + " returned invalid NFT serials; reconcile it before retrying.");
        return serials;
        }
        catch(Exception error)
        {
            throw new HederaNativeMutationReconciliationException("NFT mint receipt validation failed; reconcile original transaction " + requestedId,
                new HederaNativeMutationEvidence(requestedId,null,requestedId,"mint-receipt-validation",verifiedSuccess?"SUCCESS":null),error);
        }
    }

    private static void RecordHederaNftDelivery(IWeb3NFTTransactionResponse response, string transactionId, string recipient)
    {
        if (response?.Web3NFT == null || string.IsNullOrWhiteSpace(transactionId))
            throw new InvalidOperationException("A confirmed NFT delivery requires its original transaction ID and minted NFT.");
        response.SendNFTTransactionResult = transactionId;
        void Update(IWeb3NFT nft)
        {
            nft.SendNFTTransactionHash = transactionId;
            nft.SendToAddressAfterMinting = recipient;
        }
        Update(response.Web3NFT);
        if (response.Web3NFTs != null)
            foreach (var nft in response.Web3NFTs.Where(nft => !ReferenceEquals(nft, response.Web3NFT))) Update(nft);
    }
}
