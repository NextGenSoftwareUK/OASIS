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
            if (request.Web3NFTId != Guid.Empty && request.Web3NFTId != metadata.Id)
                throw new ArgumentException("OASIS NFT ID does not match the escrow metadata.");
            var committed = await InvokeSdkAsync("nftUnlock", new { tokenId = request.NFTTokenAddress, privateKey = key });
            var loaded = await LoadOnChainNFTDataAsync(request.NFTTokenAddress);
            if (loaded.IsError) throw new InvalidOperationException("Unlock committed but NFT readback failed: " + loaded.Message);
            result.Result = new Web3NFTTransactionResponse { TransactionResult = committed.GetProperty("transactionHash").GetString(), Web3NFT = loaded.Result };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    private async Task<string> MintRecipientAsync(IMintWeb3NFTRequest request, string signer)
    {
        if (!string.IsNullOrWhiteSpace(request.SendToAddressAfterMinting)) return request.SendToAddressAfterMinting;
        Core.Interfaces.IProviderWallet recipient = null;
        if (request.SendToAvatarAfterMintingId != Guid.Empty)
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.SendToAvatarAfterMintingId, Core.Enums.ProviderType.SuiOASIS);
            if (wallet.IsError || wallet.Result == null) throw new InvalidOperationException(wallet.Message);
            recipient = wallet.Result;
        }
        else if (!string.IsNullOrWhiteSpace(request.SendToAvatarAfterMintingUsername))
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByUsernameAsync(request.SendToAvatarAfterMintingUsername, providerType: Core.Enums.ProviderType.SuiOASIS);
            if (wallet.IsError || wallet.Result == null) throw new InvalidOperationException(wallet.Message);
            recipient = wallet.Result;
        }
        else if (!string.IsNullOrWhiteSpace(request.SendToAvatarAfterMintingEmail))
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByEmailAsync(request.SendToAvatarAfterMintingEmail, Core.Enums.ProviderType.SuiOASIS);
            if (wallet.IsError || wallet.Result == null) throw new InvalidOperationException(wallet.Message);
            recipient = wallet.Result;
        }
        else return signer;
        if (string.IsNullOrWhiteSpace(recipient.WalletAddress)) throw new InvalidOperationException("Recipient Sui wallet has no address.");
        return recipient.WalletAddress;
    }

    public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        => MintNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.NFTStandardType.HasValue)
                throw new ArgumentException("Sui address-owned objects are not ERC721/ERC1155/SPL assets.");
            if (!string.IsNullOrEmpty(request.MemoText)) throw new ArgumentException("Sui NFT transactions have no memo field.");
            var signer = await InvokeSdkAsync("restoreKey");
            var creator = signer.GetProperty("address").GetString();
            var recipient = await MintRecipientAsync(request, creator);
            var metadata = new Web3NFT
            {
                Title = request.Title, Description = request.Description, Symbol = request.Symbol,
                JSONMetaData = request.JSONMetaData, JSONMetaDataURL = request.JSONMetaDataURL,
                ImageUrl = request.ImageUrl, ThumbnailUrl = request.ThumbnailUrl,
                Image = request.Image, Thumbnail = request.Thumbnail, Tags = request.Tags?.ToList() ?? new List<string>(),
                MetaData = request.MetaData == null ? new Dictionary<string, string>() : new Dictionary<string, string>(request.MetaData),
                NFTMintedUsingWalletAddress = creator, OASISMintWalletAddress = creator,
                MintedByAvatarId = request.MintedByAvatarId, MintedOn = DateTime.UtcNow,
                CollectionPublicKey = string.IsNullOrWhiteSpace(request.CollectionPublicKey) ? _contractAddress : request.CollectionPublicKey,
                SendToAddressAfterMinting = recipient, StoreNFTMetaDataOnChain = true,
                OnChainProvider = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SuiOASIS)
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
                packageAddress = metadata.CollectionPublicKey, recipient, count,
                metadata = records.Select(OasisJson.Serialize).ToArray()
            });
            var hash = committed.GetProperty("transactionHash").GetString();
            var nfts = new List<IWeb3NFT>();
            foreach (var token in committed.GetProperty("tokenIds").EnumerateArray())
            {
                var value = await InvokeSdkAsync("nftGet", new { tokenId = token.GetString(), packageAddress = metadata.CollectionPublicKey });
                var nft = OasisJson.Deserialize<Web3NFT>(value.GetProperty("metadata").GetString());
                nft.NFTTokenAddress = token.GetString();
                nft.MintTransactionHash = hash;
                nfts.Add(nft);
            }
            result.Result = new Web3NFTTransactionResponse { TransactionResult = hash, Web3NFT = nfts.First(), Web3NFTs = nfts };
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
            var tokenId = request.TokenId ?? request.FromNFTTokenAddress ?? request.TokenAddress;
            var committed = await InvokeSdkAsync("nftSend", new { tokenId, fromWalletAddress = request.FromWalletAddress, recipient = request.ToWalletAddress });
            var loaded = await LoadOnChainNFTDataAsync(tokenId);
            if (loaded.IsError) throw new InvalidOperationException(loaded.Message);
            var hash = committed.GetProperty("transactionHash").GetString();
            loaded.Result.SendNFTTransactionHash = hash;
            result.Result = new Web3NFTTransactionResponse { TransactionResult = hash, SendNFTTransactionResult = hash, Web3NFT = loaded.Result };
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
            var committed = await InvokeSdkAsync("nftBurn", new
            {
                tokenId = request.NFTTokenAddress,
                privateKey = string.IsNullOrWhiteSpace(request.OwnerPrivateKey) ? _privateKey : request.OwnerPrivateKey
            });
            result.Result = new Web3NFTTransactionResponse { TransactionResult = committed.GetProperty("transactionHash").GetString() };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }
}
