using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
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
            var tokenId = request.NFTTokenAddress;
            var expectedId = request.Web3NFTId;
            var actor = request.LockedByAvatarId;
            string suppliedKey = null;
            if (actor != Guid.Empty)
            {
                var wallet = await LoadEscrowWalletAsync(actor);
                if (wallet.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.IProviderWallet,IWeb3NFTTransactionResponse>(wallet);
                suppliedKey = wallet.Result.PrivateKey;
            }
            var loaded = await LoadOnChainNFTDataAsync(tokenId);
            if (loaded.IsError) throw new InvalidOperationException(loaded.Message);
            if (expectedId != Guid.Empty && expectedId != loaded.Result.Id)
                throw new ArgumentException("OASIS NFT ID does not match the on-chain metadata.");
            var committed = await InvokeSdkAsync("nftLock", new { tokenId,
                privateKey = await TokenSigningKeyAsync(actor, suppliedKey) });
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
            var tokenId = request.NFTTokenAddress;
            var expectedId = request.Web3NFTId;
            var actor = request.UnlockedByAvatarId;
            string suppliedKey = null;
            if (actor != Guid.Empty)
            {
                var wallet = await LoadEscrowWalletAsync(actor);
                if (wallet.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.IProviderWallet,IWeb3NFTTransactionResponse>(wallet);
                suppliedKey = wallet.Result.PrivateKey;
            }
            var key = await TokenSigningKeyAsync(actor, suppliedKey);
            var escrow = await InvokeSdkAsync("nftEscrowGet", new { tokenId, privateKey = key });
            var metadata = OasisJson.Deserialize<Web3NFT>(escrow.GetProperty("nft").GetProperty("metadata").GetString());
            if (metadata == null) throw new InvalidOperationException("Escrow has no valid OASIS NFT metadata.");
            if (expectedId != Guid.Empty && expectedId != metadata.Id)
                throw new ArgumentException("OASIS NFT ID does not match the escrow metadata.");
            var committed = await InvokeSdkAsync("nftUnlock", new { tokenId, privateKey = key });
            var digest = committed.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(digest)) throw new InvalidOperationException("Sui unlock returned no committed digest.");
            // Retain the committed identity even when the subsequent, separate read fails.
            result.Result = new Web3NFTTransactionResponse { TransactionResult = digest, Web3NFT = metadata };
            var loaded = await LoadOnChainNFTDataAsync(tokenId);
            if (loaded.IsError || loaded.Result == null)
                throw new SuiNativeMutationReconciliationException("Unlock committed as " + digest + " but NFT readback failed; reconcile before retrying: " + loaded.Message, digest, true, loaded.Exception);
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

    public Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        => MintNFTCoreAsync(request, null, CancellationToken.None);

    /// <summary>Awaits owner persistence of verified original signed evidence before submitting the mint.
    /// The owner must durably retain evidence and reconcile uncertain outcomes; this does not authorize sponsored fees.</summary>
    public Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request,
        Func<JsonElement, CancellationToken, Task> persistPreparation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(persistPreparation);
        return MintNFTCoreAsync(request, creator => new PreparedSubmissionServer(async (evidence, token) =>
        {
            if (evidence.GetProperty("chainId").GetString() != _sdkClient.ChainId
                || evidence.GetProperty("sender").GetString() != creator)
                throw new InvalidDataException("Prepared mint chain/sender does not match the configured chain and signing wallet.");
            await VerifyOriginalSignedPreparationAsync(evidence, token);
            await persistPreparation(evidence.Clone(), token);
        }), cancellationToken);
    }

    /// <summary>Retains the mint's original signed attempt in the existing admitted execution outbox before submission.
    /// Receipt measurement and settlement remain the execution host's responsibility.</summary>
    public Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request,
        UsageExecutionHandle execution, string clientRequestId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientRequestId);
        return MintNFTCoreAsync(request,
            creator => CreateNativePreparationServer(execution, clientRequestId, _sdkClient.ChainId, creator), cancellationToken);
    }

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTCoreAsync(IMintWeb3NFTRequest request,
        Func<string, PreparedSubmissionServer> createPreparationServer, CancellationToken cancellationToken)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (createPreparationServer != null && string.IsNullOrWhiteSpace(_sdkClient.ChainId))
                throw new InvalidOperationException("Prepared mint ownership requires the configured Sui chain identity.");
            var count = request.NumberToMint ?? 1;
            NftMetadataProjection.Validate(request.StoreNFTMetaDataOnChain, request.JSONMetaDataURL);
            if (count < 1) throw new ArgumentException("Positive NFT mint count is required.");
            if (request.NFTStandardType.HasValue && request.NFTStandardType != Core.Enums.NFTStandardType.SuiObject)
                throw new ArgumentException("Sui address-owned objects are not ERC721/ERC1155/SPL assets.");
            if (request.OnChainProvider.HasValue && request.OnChainProvider != Core.Enums.ProviderType.SuiOASIS
                && request.OnChainProvider != Core.Enums.ProviderType.Default)
                throw new ArgumentException("OnChainProvider must select SuiOASIS for this provider.");
            if (request.Price < 0 || request.Discount < 0 || request.RoyaltyPercentage < 0 || request.RoyaltyPercentage > 100)
                throw new ArgumentException("Price/discount must be nonnegative and royalty percentage must be between 0 and 100.");
            if (request.SaleStartDate.HasValue && request.SaleEndDate.HasValue && request.SaleStartDate > request.SaleEndDate)
                throw new ArgumentException("Sale start must not follow sale end.");
            var actor = request.MintedByAvatarId;
            string suppliedKey = null;
            if (actor != Guid.Empty)
            {
                var wallet = await LoadEscrowWalletAsync(actor);
                if (wallet.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.IProviderWallet,IWeb3NFTTransactionResponse>(wallet);
                suppliedKey = wallet.Result.PrivateKey;
            }
            var signingKey = await TokenSigningKeyAsync(actor, suppliedKey);
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
                SendToAddressAfterMinting = recipient, StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain != false,
                OnChainProvider = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SuiOASIS),
                NFTStandardType = new EnumValue<Core.Enums.NFTStandardType>(Core.Enums.NFTStandardType.SuiObject)
            };
            var records = new List<Web3NFT>();
            for (var i = 0; i < count; i++)
            {
                var record = OasisJson.Deserialize<Web3NFT>(OasisJson.Serialize(metadata));
                record.Id = Guid.NewGuid();
                records.Add(record);
            }
            using var preparationServer = createPreparationServer?.Invoke(creator);
            var mintArguments = new
            {
                packageAddress = metadata.CollectionPublicKey, privateKey = signingKey, recipient, count,
                metadata = records.Select(record => OasisJson.Serialize(NftMetadataProjection.ForOnChainStorage(record))).ToArray()
            };
            var committed = preparationServer == null
                ? await InvokeSdkAsync("nftMint", mintArguments, cancellationToken)
                : await _sdkClient.InvokePreparedAsync("nftMint", mintArguments, preparationServer, cancellationToken);
            var hash = committed.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(hash)) throw new InvalidOperationException("Sui mint returned no committed digest.");
            var nfts = new List<IWeb3NFT>();
            result.Result = new Web3NFTTransactionResponse
            {
                TransactionResult = hash, SendNFTTransactionResult = hash, Web3NFTs = nfts
            };
            try
            {
            var tokenIds = committed.GetProperty("tokenIds").EnumerateArray().Select(token => token.GetString()).ToList();
            if (tokenIds.Count != records.Count || tokenIds.Any(string.IsNullOrWhiteSpace)
                || tokenIds.Distinct(StringComparer.Ordinal).Count() != tokenIds.Count)
                throw new InvalidOperationException("Mint receipt must identify every requested NFT exactly once.");
            if (!SameSuiAddress(committed.GetProperty("recipient").GetString(), recipient))
                throw new InvalidOperationException("Mint receipt recipient does not match the requested recipient.");
            var remainingIds = records.Select(record => record.Id).ToHashSet();
            foreach (var tokenId in tokenIds)
            {
                var value = await InvokeSdkAsync("nftGet", new { tokenId, packageAddress = metadata.CollectionPublicKey });
                var nft = OasisJson.Deserialize<Web3NFT>(value.GetProperty("metadata").GetString());
                if (nft == null || !remainingIds.Remove(nft.Id))
                    throw new InvalidOperationException("Mint readback identity is missing, foreign or duplicated.");
                if (value.GetProperty("creator").GetString() != creator)
                    throw new InvalidOperationException("Mint readback creator does not match the signing wallet.");
                if (!metadata.StoreNFTMetaDataOnChain)
                {
                    if (nft.StoreNFTMetaDataOnChain || nft.JSONMetaDataURL != metadata.JSONMetaDataURL)
                        throw new InvalidOperationException("Mint readback does not match the submitted off-chain URI projection.");
                    // Return the original request data only after binding native identity and URI.
                    // Standalone on-chain reads deliberately return just the stored projection.
                    nft = records.Single(record => record.Id == nft.Id);
                }
                nft.NFTTokenAddress = tokenId;
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
                throw new SuiNativeMutationReconciliationException("Mint committed as " + hash + " but NFT readback failed; reconcile before retrying: " + ex.Message, hash, true, ex);
            }
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string address)
        => LoadOnChainNFTDataAsync(address).GetAwaiter().GetResult();

    // The official SDK normalizes hexadecimal Sui addresses to 32 bytes in receipts.
    // Syntax validation remains at the SDK boundary; compare equivalent address spellings here.
    private static bool SameSuiAddress(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        static string Canonical(string address)
        {
            if (address.StartsWith("0x", StringComparison.Ordinal) && address.Length > 2 && address.Length <= 66
                && address.AsSpan(2).ToArray().All(Uri.IsHexDigit))
                return "0x" + address.Substring(2).PadLeft(64, '0').ToLowerInvariant();
            return address;
        }
        return StringComparer.Ordinal.Equals(Canonical(left), Canonical(right));
    }

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
                throw new SuiNativeMutationReconciliationException("NFT transfer committed as " + hash + " but readback failed; reconcile before retrying: " + loaded.Message, hash, true, loaded.Exception);
            if (!string.Equals(loaded.Result.SendToAddressAfterMinting, request.ToWalletAddress, StringComparison.OrdinalIgnoreCase))
                throw new SuiNativeMutationReconciliationException("NFT transfer committed as " + hash + " but current owner does not match the requested recipient; reconcile before retrying.",
                    hash, true, new InvalidDataException("Post-transfer NFT ownership differs from the requested delivery."));
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
            var tokenId = request.NFTTokenAddress;
            var expectedId = request.Web3NFTId;
            var actor = request.BurntByAvatarId;
            var suppliedKey = request.OwnerPrivateKey;
            var seed = request.OwnerSeedPhrase;
            var publicKey = request.OwnerPublicKey;
            if (actor != Guid.Empty && string.IsNullOrWhiteSpace(suppliedKey) && string.IsNullOrWhiteSpace(seed))
            {
                var wallet = await LoadEscrowWalletAsync(actor);
                if (wallet.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.IProviderWallet,IWeb3NFTTransactionResponse>(wallet);
                suppliedKey = wallet.Result.PrivateKey;
            }
            var loaded = await LoadOnChainNFTDataAsync(tokenId);
            if (loaded.IsError) throw new InvalidOperationException(loaded.Message);
            if (expectedId != Guid.Empty && expectedId != loaded.Result.Id)
                throw new ArgumentException("OASIS NFT ID does not match the on-chain metadata.");
            var key = await TokenSigningKeyAsync(actor, suppliedKey, seed, publicKey);
            var committed = await InvokeSdkAsync("nftBurn", new
            {
                tokenId,
                privateKey = key
            });
            result.Result = new Web3NFTTransactionResponse { TransactionResult = committed.GetProperty("transactionHash").GetString() };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }
}
