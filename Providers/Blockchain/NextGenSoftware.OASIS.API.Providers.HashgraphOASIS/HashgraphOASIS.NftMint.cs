using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using ChainProvider = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS
{
    private async Task<OASISResult<IWeb3NFTTransactionResponse>> MintHederaNftAsync(IMintWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var tokenId = request.CollectionPublicKey;
            if (!System.Text.RegularExpressions.Regex.IsMatch(tokenId ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                throw new ArgumentException("CollectionPublicKey must identify an existing HTS NFT token.");
            if (request.OnChainProvider.HasValue && request.OnChainProvider.Value != ChainProvider.HashgraphOASIS)
                throw new ArgumentException("The requested on-chain provider must be HashgraphOASIS.");
            if (request.NFTStandardType.HasValue)
                throw new ArgumentException("The shared NFT standard enum does not yet expose native HTS; do not request another chain's standard.");
            if (request.FreezeMetadata == true || request.RoyaltyPercentage.GetValueOrDefault() != 0)
                throw new ArgumentException("Metadata authority and royalty changes require an HTS collection operation, not an NFT mint.");
            var count = request.NumberToMint ?? 1;
            if (count < 1 || count > 10)
                throw new ArgumentException("This HTS mint transaction supports 1 to 10 NFT serials; larger batches require separate transactions.");
            if (request.Amount != 0 && request.Amount != count)
                throw new ArgumentException("Amount must agree with NumberToMint when supplied for an NFT mint.");
            var metadata = request.StoreNFTMetaDataOnChain == true ? request.JSONMetaData : request.JSONMetaDataURL;
            if (string.IsNullOrWhiteSpace(metadata) || Encoding.UTF8.GetByteCount(metadata) > 100)
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
            if (targets.Any(target => !System.Text.RegularExpressions.Regex.IsMatch(target ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
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
            foreach (var serial in serials) ParseHederaNftAddress(tokenId + "/" + serial);
            var nft = new Web3NFT
            {
                NFTTokenAddress = tokenId + "/" + serials[0], CollectionPublicKey = tokenId, MintTransactionHash = mintId,
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
            result.Result = new Web3NFTTransactionResponse { TransactionResult = mintId, Web3NFT = nft };
            var recipient = targets.FirstOrDefault();
            if (recipient != null && recipient != wallet.WalletAddress)
            {
                var sent = await HederaSdkClient.InvokeAsync("nftTransfer", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, sender = wallet.WalletAddress, recipient,
                    tokenId, serials, privateKey = wallet.PrivateKey, publicKey = wallet.PublicKey, keyType = "der", memo = request.MemoText,
                    transactionId = HederaSdkClient.CreateTransactionId(wallet.WalletAddress)
                });
                if (sent.GetProperty("status").GetString() != "SUCCESS") throw new InvalidOperationException("Mint completed but recipient transfer has no successful receipt.");
                RecordHederaNftDelivery(result.Result, sent.GetProperty("transactionId").GetString(), recipient);
            }
            result.Message = "HTS NFT mint and requested recipient transfer confirmed by consensus receipts.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Hedera NFT mint failed: {ex.Message}", ex);
        }
        return result;
    }

    private static string[] ApplyHederaMintReceipt(IWeb3NFTTransactionResponse response,
        System.Text.Json.JsonElement receipt, string requestedId, int count)
    {
        if (receipt.GetProperty("status").GetString() != "SUCCESS"
            || receipt.GetProperty("transactionId").GetString() != requestedId)
            throw new InvalidOperationException("NFT mint receipt identity/status mismatch; reconcile the original transaction " + requestedId);
        response.TransactionResult = requestedId;
        var serials = receipt.GetProperty("serials").EnumerateArray().Select(value => value.GetString()).ToArray();
        if (serials.Length != count || serials.Distinct(StringComparer.Ordinal).Count() != count
            || serials.Any(value => !long.TryParse(value, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var serial) || serial <= 0))
            throw new InvalidOperationException("Mint " + requestedId + " returned invalid NFT serials; reconcile it before retrying.");
        return serials;
    }

    private static void RecordHederaNftDelivery(IWeb3NFTTransactionResponse response, string transactionId, string recipient)
    {
        if (response?.Web3NFT == null || string.IsNullOrWhiteSpace(transactionId))
            throw new InvalidOperationException("A confirmed NFT delivery requires its original transaction ID and minted NFT.");
        response.SendNFTTransactionResult = transactionId;
        response.Web3NFT.SendNFTTransactionHash = transactionId;
        response.Web3NFT.SendToAddressAfterMinting = recipient;
    }
}
