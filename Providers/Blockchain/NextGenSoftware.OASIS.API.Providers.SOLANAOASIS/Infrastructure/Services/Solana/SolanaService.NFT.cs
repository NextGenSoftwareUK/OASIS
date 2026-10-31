using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Requests;
using Solnet.Metaplex.Utilities;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public sealed partial class SolanaService
{
    public MetadataClient MetadataClient => new(rpcClient);

    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        OASISResult<decimal> result = new OASISResult<decimal>();
        string errorMessage = "Error occured in SolanaService calling GetAccountBalance. Reason: ";

        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                throw new ArgumentException("A Solana wallet address is required.", nameof(request));
            var address = new PublicKey(request.WalletAddress);
            // Force address decoding before any RPC; Solnet string constructors are lazy.
            if (address.KeyBytes.Length != 32) throw new ArgumentException("Invalid Solana wallet address.", nameof(request));
            var balance = await rpcClient.GetBalanceAsync(address.Key, Solnet.Rpc.Types.Commitment.Finalized);
            if (!balance.WasSuccessful)
                throw new InvalidOperationException($"Solana balance RPC failed: {balance.Reason}");
            if (balance.Result == null) throw new System.IO.InvalidDataException("Solana balance RPC returned no result.");
            result.Result = balance.Result.Value / Lamports;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}", e);
        }

        return result;
    }

    public async Task<OASISResult<MintNftResult>> MintNftAsync(IMintWeb3NFTRequest mintNftRequest)
    {
        FinalizedSolanaRpc submissionState=null;
        try
        {
            ArgumentNullException.ThrowIfNull(mintNftRequest);
            var royaltyPercentage=mintNftRequest.RoyaltyPercentage;
            if(royaltyPercentage is <0 or >100)
                throw new ArgumentOutOfRangeException(nameof(mintNftRequest.RoyaltyPercentage),"NFT royalty percentage must be between 0 and 100 inclusive.");
            var royaltyBasisPoints=royaltyPercentage.HasValue?checked((uint)royaltyPercentage.Value*100):SellerFeeBasisPoints;
            string verifyTransaction = null;
            string verifyWarning = null;
            Account mintAccount = new();

            Metadata tokenMetadata = new()
            {
                name = mintNftRequest.Title,
                symbol = mintNftRequest.Symbol,
                sellerFeeBasisPoints = royaltyBasisPoints,
                uri = mintNftRequest.JSONMetaDataURL,
                creators = _creators,
                collection = string.IsNullOrEmpty(mintNftRequest.CollectionPublicKey) ? null : new Collection(new PublicKey(mintNftRequest.CollectionPublicKey))
            };

            {
                var finalized=FinalizedSolanaRpc.Wrap(rpcClient,oasisAccount);
                submissionState=finalized.State;
                RequestResult<string> createNftResult = await new MetadataClient(finalized.Client).CreateNFT(
                    ownerAccount: oasisAccount,
                    mintAccount: mintAccount,
                    TokenStandard.NonFungible,
                    tokenMetadata,
                    isMasterEdition: true,
                    isMutable: mintNftRequest.FreezeMetadata != true);

                if(submissionState.SubmissionFailure!=null) throw submissionState.SubmissionFailure;

                if (!createNftResult.WasSuccessful)
                {
                    bool isBalanceError =
                        createNftResult.ErrorData?.Error.Type is TransactionErrorType.InsufficientFundsForFee
                            or TransactionErrorType.InvalidRentPayingAccount;

                    bool isLamportError = createNftResult.ErrorData?.Logs?.Any(log =>
                        log.Contains("insufficient lamports", StringComparison.OrdinalIgnoreCase)) == true;

                    if (isBalanceError || isLamportError)
                        throw new InvalidOperationException($"{createNftResult.Reason}.\n Insufficient SOL to cover the transaction fee or rent.");

                    throw new InvalidOperationException(createNftResult.Reason);
                }
                if(!submissionState.FinalizedSignatures.Contains(createNftResult.Result))
                    throw new InvalidOperationException("Metaplex SDK mint returned no independently finalized native receipt.");

                if (!string.IsNullOrEmpty(mintNftRequest.CollectionPublicKey))
                {
                    if (mintNftRequest.WaitTillNFTVerified == true)
                    {
                        var (txSig, error) = await VerifyCollectionWithRetryAsync(
                            mintNftRequest.CollectionPublicKey,
                            mintAccount.PublicKey.Key,
                            timeout: mintNftRequest.WaitForNFTToVerifyInSeconds.HasValue
                                ? TimeSpan.FromSeconds(mintNftRequest.WaitForNFTToVerifyInSeconds.Value)
                                : null,
                            retryInterval: mintNftRequest.AttemptToVerifyEveryXSeconds.HasValue
                                ? TimeSpan.FromSeconds(mintNftRequest.AttemptToVerifyEveryXSeconds.Value)
                                : null);

                        if (error != null)
                            throw new InvalidOperationException($"NFT minted (mint: {mintAccount.PublicKey.Key}) but collection verification failed: {error}");

                        verifyTransaction = txSig;
                    }
                    else
                    {
                        // Single attempt, no retries — still awaited
                        try
                        {
                            verifyTransaction = await SetAndVerifyCollectionAsync(
                                mintNftRequest.CollectionPublicKey,
                                mintAccount.PublicKey.Key);
                        }
                        catch (Exception ex)
                        {
                            if(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(ex)) throw;
                            verifyWarning = $"NFT minted successfully but collection verification failed (no retries requested): {ex.Message}. " +
                                            $"You can retry verification manually for mint: {mintAccount.PublicKey.Key}";
                        }
                    }
                }

                var result = SuccessResult(new(mintAccount.PublicKey.Key, Solana, createNftResult.Result, verifyTransaction));

                if (verifyWarning != null)
                    OASISErrorHandling.HandleWarning(ref result, verifyWarning);

                // RevokeTokenAuthorities is intentionally disabled.
                // For standard Metaplex NFTs, CreateMasterEditionV3 (called internally by CreateNFT) transfers
                // the SPL token Mint Authority and Freeze Authority to the Master Edition PDA before we get here.
                // The PDA has no private key, so SetAuthority with our wallet as signer always fails with 0x4
                // OwnerMismatch. RugCheck flagging these as DANGER is a false positive — the Master Edition
                // enforces supply=1 permanently and nobody can mint more. Nothing we can do about this score.
                // if (mintNftRequest.RevokeTokenAuthorities == true)
                // {
                //     try { await RevokeTokenAuthoritiesAsync(mintAccount.PublicKey.Key); }
                //     catch (Exception revokeEx) { OASISErrorHandling.HandleWarning(ref result, $"RevokeTokenAuthorities failed (non-fatal): {revokeEx.Message}"); }
                // }

                // FreezeMetadata is already encoded in the SDK creation instruction's isMutable.

                return result;
            }
        }
        catch (Exception ex)
        {
            var evidence=submissionState?.DescribeFailure(ex) ?? ex;
            return HandleError<MintNftResult>(evidence.Message,evidence);
        }
    }

    /// <summary>
    /// Attempts SetAndVerifyCollection every 5 seconds for up to 1 minute by default.
    /// Returns (txSignature, null) on success or (null, errorMessage) on failure.
    /// </summary>
    private async Task<(string TxSignature, string Error)> VerifyCollectionWithRetryAsync(
        string collectionMintAddress,
        string nftMintAddress,
        TimeSpan? timeout = null,
        TimeSpan? retryInterval = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromMinutes(1));
        var interval = retryInterval ?? TimeSpan.FromSeconds(5);
        string lastError = null;
        int attempt = 0;

        while (DateTime.UtcNow < deadline)
        {
            attempt++;
            try
            {
                string txSig = await SetAndVerifyCollectionAsync(collectionMintAddress, nftMintAddress);
                return (txSig, null); // success
            }
            catch (Exception ex)
            {
                if(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(ex)) throw;
                lastError = $"Attempt {attempt}: {ex.Message}";
                var remaining = deadline - DateTime.UtcNow;
                if (remaining > interval)
                    await Task.Delay(interval);
                else
                    break;
            }
        }

        return (null, $"Gave up after {attempt} attempt(s) over {(timeout ?? TimeSpan.FromMinutes(1)).TotalSeconds}s. Last error: {lastError}");
    }

    public async Task<string> SetAndVerifyCollectionAsync(string collectionMintAddress, string nftMintAddress)
    {
        PublicKey metadataProgram = new("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s");
        PublicKey nftMint         = new(nftMintAddress);
        PublicKey collectionMint  = new(collectionMintAddress);

        PublicKey nftMetadataPda             = DeriveMetadataPda(nftMint, metadataProgram);
        PublicKey collectionMetadataPda      = DeriveMetadataPda(collectionMint, metadataProgram);
        PublicKey collectionMasterEditionPda = DeriveMasterEditionPda(collectionMint, metadataProgram);

        // Instruction 32 = SetAndVerifySizedCollectionItem (deployed mpl-token-metadata program enum).
        // Instruction 25 (SetAndVerifyCollection) is blocked for sized collections with error 0x66.
        // Instruction 32 also increments collectionDetails.size, so collectionMetadata must be writable.
        byte[] instructionData = new byte[] { 32 };

        List<AccountMeta> accounts = new()
        {
            AccountMeta.Writable(nftMetadataPda, false),              // 0 NFT metadata
            AccountMeta.Writable(oasisAccount.PublicKey, true),       // 1 collection authority (signer)
            AccountMeta.Writable(oasisAccount.PublicKey, true),       // 2 payer (signer)
            AccountMeta.ReadOnly(oasisAccount.PublicKey, false),      // 3 update authority of NFT + collection
            AccountMeta.ReadOnly(collectionMint, false),              // 4 collection mint
            AccountMeta.Writable(collectionMetadataPda, false),       // 5 collection metadata (writable — increments size)
            AccountMeta.ReadOnly(collectionMasterEditionPda, false),  // 6 collection master edition
        };

        TransactionInstruction verifyInstruction = new()
        {
            ProgramId = metadataProgram.KeyBytes,
            Keys = accounts,
            Data = instructionData
        };

        var blockHash = await rpcClient.GetLatestBlockHashAsync();
        if (!blockHash.WasSuccessful || blockHash.Result?.Value == null)
            throw new Exception($"Failed to get latest blockhash: {blockHash.Reason}");

        byte[] txBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(verifyInstruction)
            .Build(oasisAccount);

        // Simulate first so we can surface the actual program error logs if it fails
        var simResult = await rpcClient.SimulateTransactionAsync(txBytes);
        if(simResult==null || !simResult.WasSuccessful || simResult.Result?.Value==null)
            throw new InvalidOperationException("SetAndVerifyCollection simulation result is unavailable; no transaction submitted.");
        if (simResult.Result.Value.Error != null)
        {
            string logs = simResult.Result.Value.Logs != null
                ? string.Join(" | ", simResult.Result.Value.Logs)
                : "no logs";
            throw new Exception($"SetAndVerifyCollection simulation failed [ix={string.Join(",", instructionData)} accounts={accounts.Count}]: {simResult.Result.Value.Error} — logs: {logs}");
        }

        return await new NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories.SolanaRepository(
            oasisAccount,rpcClient).SubmitSignedTransactionAsync(txBytes);
    }

    // DISABLED — see comment at call site in MintNftAsync for explanation.
    // private async Task RevokeTokenAuthoritiesAsync(string nftMintAddress) { ... }

    // PDA derivation — seeds: ["metadata", programId, mintPubkey]
    private static PublicKey DeriveMetadataPda(PublicKey mint, PublicKey metadataProgram)
    {
        bool success = PublicKey.TryFindProgramAddress(
            new[]
            {
            System.Text.Encoding.UTF8.GetBytes("metadata"),
            metadataProgram.KeyBytes,
            mint.KeyBytes
            },
            metadataProgram,
            out PublicKey pda,
            out _);

        if (!success) throw new Exception($"Failed to derive metadata PDA for {mint}");
        return pda;
    }

    // PDA derivation — seeds: ["metadata", programId, mintPubkey, "edition"]
    private static PublicKey DeriveMasterEditionPda(PublicKey mint, PublicKey metadataProgram)
    {
        bool success = PublicKey.TryFindProgramAddress(
            new[]
            {
            System.Text.Encoding.UTF8.GetBytes("metadata"),
            metadataProgram.KeyBytes,
            mint.KeyBytes,
            System.Text.Encoding.UTF8.GetBytes("edition")
            },
            metadataProgram,
            out PublicKey pda,
            out _);

        if (!success) throw new Exception($"Failed to derive master edition PDA for {mint}");
        return pda;
    }

    public async Task<OASISResult<CreateCollectionNftResult>> CreateCollectionNftAsync(
        string name, string symbol, string metadataUri, ulong initialSize = 0, bool freezeMetadata = false)
    {
        FinalizedSolanaRpc submissionState=null;
        try
        {
            Account collectionMintAccount = new();

            Metadata tokenMetadata = new()
            {
                name = name,
                symbol = symbol,
                sellerFeeBasisPoints = SellerFeeBasisPoints,
                uri = metadataUri,
                creators = _creators,
                collection = null  // collection NFTs have no parent
            };

            {
                var finalized=FinalizedSolanaRpc.Wrap(rpcClient,oasisAccount);
                submissionState=finalized.State;
                RequestResult<string> createResult = await new MetadataClient(finalized.Client).CreateNFT(
                    ownerAccount: oasisAccount,
                    mintAccount: collectionMintAccount,
                    TokenStandard.NonFungible,
                    tokenMetadata,
                    isMasterEdition: true,
                    isMutable: !freezeMetadata);
                if(submissionState.SubmissionFailure!=null) throw submissionState.SubmissionFailure;

                if (!createResult.WasSuccessful)
                {
                    bool isBalanceError =
                        createResult.ErrorData?.Error.Type is TransactionErrorType.InsufficientFundsForFee
                            or TransactionErrorType.InvalidRentPayingAccount;

                    bool isLamportError = createResult.ErrorData?.Logs?.Any(log =>
                        log.Contains("insufficient lamports", StringComparison.OrdinalIgnoreCase)) == true;

                    if (isBalanceError || isLamportError)
                        throw new InvalidOperationException($"{createResult.Reason}.\n Insufficient SOL to cover the transaction fee or rent.");

                    throw new InvalidOperationException(createResult.Reason);
                }
                if(!submissionState.FinalizedSignatures.Contains(createResult.Result))
                    throw new InvalidOperationException("Metaplex SDK collection mint returned no independently finalized native receipt.");

                // Immediately set collectionDetails so Phantom/DAS API recognises this as a collection parent.
                string setSizeTxHash = await SetCollectionSizeAsync(collectionMintAccount.PublicKey.Key, initialSize);

                return new OASISResult<CreateCollectionNftResult>
                {
                    IsSaved = true,
                    IsError = false,
                    Result = new CreateCollectionNftResult(
                        collectionMintAccount.PublicKey.Key,
                        Solana,
                        createResult.Result,
                        setSizeTxHash)
                };
            }
        }
        catch (Exception ex)
        {
            var evidence=submissionState?.DescribeFailure(ex) ?? ex;
            return HandleError<CreateCollectionNftResult>(evidence.Message,evidence);
        }
    }
}
