using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public partial class SuiOASIS
{
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        => LockNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var loaded = await LoadOnChainNFTDataAsync(request.NFTTokenAddress);
            if (loaded.IsError) throw new InvalidOperationException(loaded.Message);
            if (request.Web3NFTId != Guid.Empty && request.Web3NFTId != loaded.Result.Id)
                throw new ArgumentException("OASIS NFT ID does not match the on-chain metadata.");
            var committed = await InvokeSdkAsync("nftLock", new { tokenId = request.NFTTokenAddress,
                privateKey = await TokenSigningKeyAsync(request.LockedByAvatarId) });
            result.Result = new Web3NFTTransactionResponse { TransactionResult = committed.GetProperty("transactionHash").GetString(), Web3NFT = loaded.Result };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        => UnlockNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var key = await TokenSigningKeyAsync(request.UnlockedByAvatarId);
            var escrow = await InvokeSdkAsync("nftEscrowGet", new { tokenId = request.NFTTokenAddress, privateKey = key });
            var metadata = OasisJson.Deserialize<Web3NFT>(escrow.GetProperty("nft").GetProperty("metadata").GetString());
            if (metadata == null) throw new InvalidOperationException("Escrow has no valid OASIS NFT metadata.");
            if (request.Web3NFTId != Guid.Empty && request.Web3NFTId != metadata.Id)
                throw new ArgumentException("OASIS NFT ID does not match the escrow metadata.");
            var committed = await InvokeSdkAsync("nftUnlock", new { tokenId = request.NFTTokenAddress, privateKey = key });
            var digest = committed.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(digest)) throw new InvalidOperationException("Sui unlock returned no committed digest.");
            // Retain the committed identity even when the subsequent, separate read fails.
            result.Result = new Web3NFTTransactionResponse { TransactionResult = digest, Web3NFT = metadata };
            var loaded = await LoadOnChainNFTDataAsync(request.NFTTokenAddress);
            if (loaded.IsError || loaded.Result == null)
                throw new InvalidOperationException("Unlock committed as " + digest + " but NFT readback failed; reconcile before retrying: " + loaded.Message, loaded.Exception);
            result.Result.Web3NFT = loaded.Result;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    private async Task<string> MintRecipientAsync(IMintWeb3NFTRequest request, string signer)
    {
        var addresses = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.SendToAddressAfterMinting)) addresses.Add(request.SendToAddressAfterMinting);
        if (request.SendToAvatarAfterMintingId != Guid.Empty)
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.SendToAvatarAfterMintingId, Core.Enums.ProviderType.SuiOASIS);
            if (wallet.IsError || wallet.Result == null) throw new InvalidOperationException(wallet.Message);
            addresses.Add(wallet.Result.WalletAddress);
        }
        if (!string.IsNullOrWhiteSpace(request.SendToAvatarAfterMintingUsername))
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByUsernameAsync(request.SendToAvatarAfterMintingUsername, providerType: Core.Enums.ProviderType.SuiOASIS);
            if (wallet.IsError || wallet.Result == null) throw new InvalidOperationException(wallet.Message);
            addresses.Add(wallet.Result.WalletAddress);
        }
        if (!string.IsNullOrWhiteSpace(request.SendToAvatarAfterMintingEmail))
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByEmailAsync(request.SendToAvatarAfterMintingEmail, Core.Enums.ProviderType.SuiOASIS);
            if (wallet.IsError || wallet.Result == null) throw new InvalidOperationException(wallet.Message);
            addresses.Add(wallet.Result.WalletAddress);
        }
        if (addresses.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Recipient Sui wallet has no address.");
        if (addresses.Distinct(StringComparer.Ordinal).Count() > 1)
            throw new ArgumentException("Recipient address and avatar identifiers resolve to different Sui wallets.");
        return addresses.Count == 0 ? signer : addresses[0];
    }

    public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        => MintNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.NFTStandardType.HasValue && request.NFTStandardType != Core.Enums.NFTStandardType.SuiObject)
                throw new ArgumentException("Sui address-owned objects are not ERC721/ERC1155/SPL assets.");
            if (request.OnChainProvider.HasValue && request.OnChainProvider != Core.Enums.ProviderType.SuiOASIS
                && request.OnChainProvider != Core.Enums.ProviderType.Default)
                throw new ArgumentException("OnChainProvider must select SuiOASIS for this provider.");
            if (request.Price < 0 || request.Discount < 0 || request.RoyaltyPercentage < 0 || request.RoyaltyPercentage > 100)
                throw new ArgumentException("Price/discount must be nonnegative and royalty percentage must be between 0 and 100.");
            if (request.SaleStartDate.HasValue && request.SaleEndDate.HasValue && request.SaleStartDate > request.SaleEndDate)
                throw new ArgumentException("Sale start must not follow sale end.");
            var signingKey = await TokenSigningKeyAsync(request.MintedByAvatarId);
            var signer = await InvokeSdkAsync("restoreKey", new { privateKey = signingKey });
            var creator = signer.GetProperty("address").GetString();
            var recipient = await MintRecipientAsync(request, creator);
            var metadata = new Web3NFT
            {
                Title = request.Title, Description = request.Description, Symbol = request.Symbol, MemoText = request.MemoText,
                JSONMetaData = request.JSONMetaData, JSONMetaDataURL = request.JSONMetaDataURL,
                ImageUrl = request.ImageUrl, ThumbnailUrl = request.ThumbnailUrl,
                Image = request.Image, Thumbnail = request.Thumbnail, Tags = request.Tags?.ToList() ?? new List<string>(),
                MetaData = request.MetaData == null ? new Dictionary<string, string>() : new Dictionary<string, string>(request.MetaData),
                NFTMintedUsingWalletAddress = creator, OASISMintWalletAddress = creator,
                MintedByAvatarId = request.MintedByAvatarId, MintedOn = DateTime.UtcNow,
                Price = request.Price ?? 0, Discount = request.Discount ?? 0,
                RoyaltyPercentage = request.RoyaltyPercentage ?? 0,
                SellerFeeBasisPoints = checked((uint)(request.RoyaltyPercentage ?? 0) * 100),
                IsForSale = request.IsForSale ?? false, SaleStartDate = request.SaleStartDate, SaleEndDate = request.SaleEndDate,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                CollectionPublicKey = string.IsNullOrWhiteSpace(request.CollectionPublicKey) ? _contractAddress : request.CollectionPublicKey,
                SendToAddressAfterMinting = recipient, StoreNFTMetaDataOnChain = true,
                OnChainProvider = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SuiOASIS),
                NFTStandardType = new EnumValue<Core.Enums.NFTStandardType>(Core.Enums.NFTStandardType.SuiObject)
            };
            var count = request.NumberToMint ?? 1;
            if (count < 1) throw new ArgumentException("Positive NFT mint count is required.");
            var records = new List<Web3NFT>();
            for (var i = 0; i < count; i++)
            {
                var record = OasisJson.Deserialize<Web3NFT>(OasisJson.Serialize(metadata));
                record.Id = Guid.NewGuid();
                records.Add(record);
            }
            var committed = await InvokeSdkAsync("nftMint", new
            {
                packageAddress = metadata.CollectionPublicKey, privateKey = signingKey, recipient, count,
                metadata = records.Select(OasisJson.Serialize).ToArray()
            });
            var hash = committed.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(hash)) throw new InvalidOperationException("Sui mint returned no committed digest.");
            var nfts = new List<IWeb3NFT>();
            result.Result = new Web3NFTTransactionResponse
            {
                TransactionResult = hash, SendNFTTransactionResult = hash, Web3NFTs = nfts
            };
            try
            {
            foreach (var token in committed.GetProperty("tokenIds").EnumerateArray())
            {
                var value = await InvokeSdkAsync("nftGet", new { tokenId = token.GetString(), packageAddress = metadata.CollectionPublicKey });
                var nft = OasisJson.Deserialize<Web3NFT>(value.GetProperty("metadata").GetString());
                nft.NFTTokenAddress = token.GetString();
                nft.MintTransactionHash = hash;
                var actualOwner = value.GetProperty("owner").GetProperty("AddressOwner").GetString();
                if (actualOwner != committed.GetProperty("recipient").GetString())
                    throw new InvalidOperationException("Mint receipt and on-chain NFT recipient do not match.");
                nft.SendToAddressAfterMinting = actualOwner;
                // The signed Move mint transaction also transfers the object to its recipient.
                nft.SendNFTTransactionHash = hash;
                nfts.Add(nft);
            }
            result.Result.Web3NFT = nfts.First();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Mint committed as " + hash + " but NFT readback failed; reconcile before retrying: " + ex.Message, ex);
            }
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string address)
        => LoadOnChainNFTDataAsync(address).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        var result = new OASISResult<IWeb3NFT>();
        try
        {
            var value = await InvokeSdkAsync("nftGet", new { tokenId = nftTokenAddress });
            var nft = OasisJson.Deserialize<Web3NFT>(value.GetProperty("metadata").GetString());
            if (nft == null) throw new InvalidOperationException("NFT contains no OASIS metadata.");
            nft.NFTTokenAddress = nftTokenAddress;
            nft.NFTMintedUsingWalletAddress = value.GetProperty("creator").GetString();
            nft.SendToAddressAfterMinting = value.GetProperty("owner").GetProperty("AddressOwner").GetString();
            result.Result = nft;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        => SendNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.Amount != 0m && request.Amount != 1m) throw new ArgumentException("A unique NFT transfer amount must be one.");
            if (!string.IsNullOrEmpty(request.MemoText)) throw new ArgumentException("Sui NFT transfers have no memo field.");
            var tokenIds = new[] { request.TokenId, request.FromNFTTokenAddress, request.TokenAddress }
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
            if (tokenIds.Length == 0) throw new ArgumentException("Sui NFT object ID is required.");
            if (tokenIds.Length != 1) throw new ArgumentException("Sui NFT token selectors identify different objects.");
            var tokenId = tokenIds[0];
            var key = await TokenSigningKeyAsync(key: request.OwnerPrivateKey, seed: request.OwnerSeedPhrase, publicKey: request.OwnerPublicKey);
            var committed = await InvokeSdkAsync("nftSend", new { tokenId, privateKey = key, fromWalletAddress = request.FromWalletAddress, recipient = request.ToWalletAddress });
            var hash = committed.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(hash)) throw new InvalidOperationException("Sui NFT transfer returned no committed digest.");
            result.Result = new Web3NFTTransactionResponse { TransactionResult = hash, SendNFTTransactionResult = hash };
            var loaded = await LoadOnChainNFTDataAsync(tokenId);
            if (loaded.IsError || loaded.Result == null)
                throw new InvalidOperationException("NFT transfer committed as " + hash + " but readback failed; reconcile before retrying: " + loaded.Message, loaded.Exception);
            loaded.Result.SendNFTTransactionHash = hash;
            result.Result.Web3NFT = loaded.Result;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        => BurnNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var loaded = await LoadOnChainNFTDataAsync(request.NFTTokenAddress);
            if (loaded.IsError) throw new InvalidOperationException(loaded.Message);
            if (request.Web3NFTId != Guid.Empty && request.Web3NFTId != loaded.Result.Id)
                throw new ArgumentException("OASIS NFT ID does not match the on-chain metadata.");
            var key = await TokenSigningKeyAsync(request.BurntByAvatarId, request.OwnerPrivateKey, request.OwnerSeedPhrase, request.OwnerPublicKey);
            var committed = await InvokeSdkAsync("nftBurn", new
            {
                tokenId = request.NFTTokenAddress,
                privateKey = key
            });
            result.Result = new Web3NFTTransactionResponse { TransactionResult = committed.GetProperty("transactionHash").GetString() };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }
}
