using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS
{
    // Canonical NFT address: CosmWasm contract address followed by '#' and token ID.
    private (string Contract, string TokenId, string Address) NftLocator(string address, Guid id = default)
    {
        if (string.IsNullOrWhiteSpace(address) && id != Guid.Empty)
            address = $"{_contractAddress}#{id:N}";
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("NFT address is required.");
        var parts = address.Split('#');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            throw new ArgumentException("NFT address must be '<contract-address>#<token-id>'.");
        return (parts[0], parts[1], address);
    }

    private async Task<string> MintRecipientAsync(IMintWeb3NFTRequest request, string signer)
    {
        if (!string.IsNullOrWhiteSpace(request.SendToAddressAfterMinting)) return request.SendToAddressAfterMinting;
        Core.Interfaces.IProviderWallet recipient = null;
        if (request.SendToAvatarAfterMintingId != Guid.Empty)
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.SendToAvatarAfterMintingId, Core.Enums.ProviderType.CosmosBlockChainOASIS);
            if (wallet.IsError || wallet.Result == null) throw new InvalidOperationException(wallet.Message);
            recipient = wallet.Result;
        }
        else if (!string.IsNullOrWhiteSpace(request.SendToAvatarAfterMintingUsername))
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByUsernameAsync(request.SendToAvatarAfterMintingUsername, providerType: Core.Enums.ProviderType.CosmosBlockChainOASIS);
            if (wallet.IsError || wallet.Result == null) throw new InvalidOperationException(wallet.Message);
            recipient = wallet.Result;
        }
        else if (!string.IsNullOrWhiteSpace(request.SendToAvatarAfterMintingEmail))
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByEmailAsync(request.SendToAvatarAfterMintingEmail, Core.Enums.ProviderType.CosmosBlockChainOASIS);
            if (wallet.IsError || wallet.Result == null) throw new InvalidOperationException(wallet.Message);
            recipient = wallet.Result;
        }
        else return signer;
        if (string.IsNullOrWhiteSpace(recipient.WalletAddress)) throw new InvalidOperationException("Recipient Cosmos wallet has no address.");
        return recipient.WalletAddress;
    }

    public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        => MintNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.NumberToMint.HasValue && request.NumberToMint != 1)
                throw new ArgumentException("This NFT call mints one unique token; use separate calls for multiple tokens.");
            if (request.NFTStandardType.HasValue)
                throw new ArgumentException("ERC721/ERC1155/SPL are not Cosmos OASIS custom-asset standards.");
            var key = await SigningKeyAsync(Guid.Empty);
            var signer = await SignerAddressAsync(key);
            var recipient = await MintRecipientAsync(request, signer);
            var contract = string.IsNullOrWhiteSpace(request.CollectionPublicKey) ? _contractAddress : request.CollectionPublicKey;
            if (string.IsNullOrWhiteSpace(contract)) throw new InvalidOperationException("CosmWasm asset contract is required.");
            var id = Guid.NewGuid();
            var locator = $"{contract}#{id:N}";
            var nft = new Web3NFT
            {
                Id = id, NFTTokenAddress = locator, CollectionPublicKey = contract,
                NFTMintedUsingWalletAddress = signer, OASISMintWalletAddress = signer,
                SendToAddressAfterMinting = recipient, Title = request.Title, Description = request.Description,
                Symbol = request.Symbol, JSONMetaData = request.JSONMetaData, JSONMetaDataURL = request.JSONMetaDataURL,
                ImageUrl = request.ImageUrl, ThumbnailUrl = request.ThumbnailUrl,
                MetaData = request.MetaData == null ? new Dictionary<string, string>() : new Dictionary<string, string>(request.MetaData),
                Tags = request.Tags?.ToList() ?? new List<string>(), MintedByAvatarId = request.MintedByAvatarId,
                MintedOn = DateTime.UtcNow, CurrentOwnerAvatarId = request.SendToAvatarAfterMintingId,
                OnChainProvider = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.CosmosBlockChainOASIS),
                StoreNFTMetaDataOnChain = true
            };
            var hash = await ExecuteAssetAsync(new { nft_mint = new
            {
                token_id = id.ToString("N"), recipient, metadata_json = OasisJson.Serialize(nft)
            } }, key, signer, contract);
            nft.MintTransactionHash = hash;
            result.Result = new Web3NFTTransactionResponse { TransactionResult = hash, Web3NFT = nft };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        => LoadOnChainNFTDataAsync(nftTokenAddress).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        var result = new OASISResult<IWeb3NFT>();
        try
        {
            var locator = NftLocator(nftTokenAddress);
            var state = await QueryAssetAsync(new { nft = new { token_id = locator.TokenId } }, locator.Contract);
            if (state.GetProperty("token_id").GetString() != locator.TokenId)
                throw new InvalidDataException("Contract returned a different NFT ID.");
            var nft = OasisJson.Deserialize<Web3NFT>(state.GetProperty("metadata_json").GetString());
            if (nft == null || nft.NFTTokenAddress != locator.Address)
                throw new InvalidDataException("NFT metadata identity does not match its on-chain address.");
            var currentOwner = state.GetProperty("owner").GetString();
            if (nft.SendToAddressAfterMinting != currentOwner) nft.CurrentOwnerAvatarId = Guid.Empty;
            nft.SendToAddressAfterMinting = currentOwner;
            nft.MetaData ??= new Dictionary<string, string>();
            nft.MetaData["CosmosLocked"] = state.GetProperty("locked").GetBoolean().ToString();
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
            if (request == null || string.IsNullOrWhiteSpace(request.FromWalletAddress) || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                throw new ArgumentException("Sender and recipient NFT wallet addresses are required.");
            var address = request.TokenAddress ?? request.FromNFTTokenAddress;
            var locator = NftLocator(address);
            if (!string.IsNullOrWhiteSpace(request.TokenId) && request.TokenId != locator.TokenId)
                throw new ArgumentException("NFT token ID conflicts with its address.");
            var key = await SigningKeyAsync(Guid.Empty);
            var hash = await TransferNftAsync(locator.Address, request.FromWalletAddress, request.ToWalletAddress, key);
            result.Result = new Web3NFTTransactionResponse { TransactionResult = hash, SendNFTTransactionResult = hash };
            var loaded = await LoadOnChainNFTDataAsync(locator.Address);
            if (loaded.IsError) throw new InvalidOperationException($"NFT transfer committed as {hash}, but readback failed: {loaded.Message}", loaded.Exception);
            loaded.Result.SendNFTTransactionHash = hash;
            result.Result.Web3NFT = loaded.Result;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    private async Task<string> TransferNftAsync(string address, string from, string recipient, string key)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Sender and recipient NFT wallet addresses are required.");
        var locator = NftLocator(address);
        return await ExecuteAssetAsync(new { nft_transfer = new { token_id = locator.TokenId, recipient } }, key, from, locator.Contract);
    }

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        => BurnNFTAsync(request).GetAwaiter().GetResult();
    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var locator = NftLocator(request.NFTTokenAddress, request.Web3NFTId);
            var key = await SigningKeyAsync(request.BurntByAvatarId, request.OwnerPrivateKey, request.OwnerSeedPhrase);
            var hash = await ExecuteAssetAsync(new { nft_burn = new { token_id = locator.TokenId } }, key, request.OwnerPublicKey, locator.Contract);
            result.Result = new Web3NFTTransactionResponse { TransactionResult = hash };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        => LockNFTAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        => ChangeNftLockAsync(request?.NFTTokenAddress, request?.Web3NFTId ?? Guid.Empty, request?.LockedByAvatarId ?? Guid.Empty, true);
    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        => UnlockNFTAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        => ChangeNftLockAsync(request?.NFTTokenAddress, request?.Web3NFTId ?? Guid.Empty, request?.UnlockedByAvatarId ?? Guid.Empty, false);

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> ChangeNftLockAsync(string address, Guid id, Guid actor, bool locked)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            var locator = NftLocator(address, id);
            var key = await SigningKeyAsync(actor);
            var hash = await ExecuteAssetAsync(new { nft_set_locked = new { token_id = locator.TokenId, locked } }, key, contractAddress: locator.Contract);
            result.Result = new Web3NFTTransactionResponse { TransactionResult = hash };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            var locator = NftLocator(nftTokenAddress);
            if (tokenId != locator.TokenId) throw new ArgumentException("NFT bridge token ID conflicts with its address.");
            var pool = await SignerAddressAsync(await SigningKeyAsync(Guid.Empty));
            var hash = await TransferNftAsync(nftTokenAddress, senderAccountAddress, pool, senderPrivateKey);
            result.Result = new BridgeTransactionResponse { TransactionId = hash, IsSuccessful = true, Status = BridgeTransactionStatus.Completed };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            var locator = NftLocator(nftTokenAddress);
            if (tokenId != locator.TokenId) throw new ArgumentException("NFT bridge token ID conflicts with its address.");
            var key = await SigningKeyAsync(Guid.Empty);
            var pool = await SignerAddressAsync(key);
            // Release the existing escrowed NFT. Cross-chain provenance is the bridge coordinator's responsibility.
            var hash = await TransferNftAsync(nftTokenAddress, pool, receiverAccountAddress, key);
            result.Result = new BridgeTransactionResponse { TransactionId = hash, IsSuccessful = true, Status = BridgeTransactionStatus.Completed };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
    {
        if (celestialBody == null || string.IsNullOrWhiteSpace(outputFolder) || string.IsNullOrWhiteSpace(nativeSource))
            throw new ArgumentException("Celestial body, output folder and generated Rust source are required.");
        var sourceDirectory = Path.Combine(Path.GetFullPath(outputFolder), "src");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(Path.Combine(sourceDirectory, "lib.rs"), nativeSource, new System.Text.UTF8Encoding(false));
        return true; // Source was actually written; compilation is a separate caller-owned step.
    }
}
