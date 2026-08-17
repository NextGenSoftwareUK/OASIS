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
            // Save the original Console.Out
            var originalConsoleOut = Console.Out;

            try
            {
                // Redirect Console.Out to a NullTextWriter to stop the SolNET Logger from outputting to the console (messes up STAR CLI!)
                Console.SetOut(new NullTextWriter());

                RequestResult<ResponseValue<AccountInfo>> solResult = await rpcClient.GetAccountInfoAsync(request.WalletAddress);

                if (solResult.WasSuccessful && solResult.Result.Value?.Lamports != null)
                {
                    decimal balanceInSol = solResult.Result.Value.Lamports / Lamports;
                    result.Result = balanceInSol;
                }

                if (!solResult.WasSuccessful)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {solResult.Reason}");
            }
            finally
            {
                // Restore the original Console.Out
                Console.SetOut(originalConsoleOut);
            }
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
        }

        return result;
    }

    public async Task<OASISResult<MintNftResult>> MintNftAsync(MintWeb3NFTRequest mintNftRequest)
    {
        try
        {
            string verifyTransaction = null;
            string verifyWarning = null;
            Account mintAccount = new();

            Metadata tokenMetadata = new()
            {
                name = mintNftRequest.Title,
                symbol = mintNftRequest.Symbol,
                sellerFeeBasisPoints = SellerFeeBasisPoints,
                uri = mintNftRequest.JSONMetaDataURL,
                creators = _creators,
                collection = string.IsNullOrEmpty(mintNftRequest.CollectionPublicKey) ? null : new Collection(new PublicKey(mintNftRequest.CollectionPublicKey))
            };

            var originalConsoleOut = Console.Out;
            try
            {
                Console.SetOut(new NullTextWriter());

                RequestResult<string> createNftResult = await MetadataClient.CreateNFT(
                    ownerAccount: oasisAccount,
                    mintAccount: mintAccount,
                    TokenStandard.NonFungible,
                    tokenMetadata,
                    isMasterEdition: true,
                    isMutable: mintNftRequest.FreezeMetadata != true);

                if (!createNftResult.WasSuccessful)
                {
                    bool isBalanceError =
                        createNftResult.ErrorData?.Error.Type is TransactionErrorType.InsufficientFundsForFee
                            or TransactionErrorType.InvalidRentPayingAccount;

                    bool isLamportError = createNftResult.ErrorData?.Logs?.Any(log =>
                        log.Contains("insufficient lamports", StringComparison.OrdinalIgnoreCase)) == true;

                    if (isBalanceError || isLamportError)
                        return HandleError<MintNftResult>($"{createNftResult.Reason}.\n Insufficient SOL to cover the transaction fee or rent.");

                    return HandleError<MintNftResult>(createNftResult.Reason);
                }

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
                            return HandleError<MintNftResult>($"NFT minted (mint: {mintAccount.PublicKey.Key}) but collection verification failed: {error}");

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

                if (mintNftRequest.FreezeMetadata == true)
                {
                    try
                    {
                        await FreezeMetadataAsync(mintAccount.PublicKey.Key);
                    }
                    catch (Exception freezeEx)
                    {
                        OASISErrorHandling.HandleWarning(ref result,
                            $"NFT minted successfully but metadata freeze failed (non-fatal): {freezeEx.Message}. " +
                            $"Run freeze-metadata.mjs manually for mint: {mintAccount.PublicKey.Key}");
                    }
                }

                return result;
            }
            finally
            {
                Console.SetOut(originalConsoleOut);
            }
        }
        catch (Exception ex)
        {
            return HandleError<MintNftResult>(ex.Message);
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

        Console.WriteLine($"[DEBUG] SetAndVerify instructionData bytes: [{string.Join(",", instructionData)}] accountCount: {accounts.Count}");

        // Simulate first so we can surface the actual program error logs if it fails
        var simResult = await rpcClient.SimulateTransactionAsync(txBytes);
        if (simResult?.Result?.Value?.Error != null)
        {
            string logs = simResult.Result.Value.Logs != null
                ? string.Join(" | ", simResult.Result.Value.Logs)
                : "no logs";
            throw new Exception($"SetAndVerifyCollection simulation failed [ix={string.Join(",", instructionData)} accounts={accounts.Count}]: {simResult.Result.Value.Error} — logs: {logs}");
        }

        RequestResult<string> sendResult = await rpcClient.SendTransactionAsync(txBytes);

        if (!sendResult.WasSuccessful)
            throw new Exception($"SetAndVerifyCollection failed: {sendResult.Reason}");

        return sendResult.Result;
    }

    // DISABLED — see comment at call site in MintNftAsync for explanation.
    // private async Task RevokeTokenAuthoritiesAsync(string nftMintAddress) { ... }

    // Sets isMutable=false on the Token Metadata account, making the NFT metadata permanently immutable.
    // Uses UpdateMetadataAccountV2 (ix 15). ONE-WAY — cannot be undone.
    private async Task FreezeMetadataAsync(string nftMintAddress)
    {
        PublicKey metadataProgram = new("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s");
        PublicKey mintPubkey = new(nftMintAddress);
        PublicKey metadataPda = DeriveMetadataPda(mintPubkey, metadataProgram);

        List<AccountMeta> accounts =
        [
            AccountMeta.Writable(metadataPda, false),             // metadata account
            AccountMeta.ReadOnly(oasisAccount.PublicKey, true),   // update authority (signer)
        ];

        // UpdateMetadataAccountV2 (ix 15), all fields None except isMutable=Some(false)
        // Borsh layout: [ix, None(data), None(update_authority), None(primary_sale_happened), Some, false]
        byte[] instructionData = [15, 0, 0, 0, 1, 0];

        TransactionInstruction freezeInstruction = new()
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
            .AddInstruction(freezeInstruction)
            .Build(oasisAccount);

        RequestResult<string> sendResult = await rpcClient.SendTransactionAsync(txBytes);

        if (!sendResult.WasSuccessful)
            throw new Exception($"FreezeMetadata failed for mint {nftMintAddress}: {sendResult.Reason}");
    }

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

            var originalConsoleOut = Console.Out;
            try
            {
                Console.SetOut(new NullTextWriter());

                RequestResult<string> createResult = await MetadataClient.CreateNFT(
                    ownerAccount: oasisAccount,
                    mintAccount: collectionMintAccount,
                    TokenStandard.NonFungible,
                    tokenMetadata,
                    isMasterEdition: true,
                    isMutable: !freezeMetadata);

                if (!createResult.WasSuccessful)
                {
                    bool isBalanceError =
                        createResult.ErrorData?.Error.Type is TransactionErrorType.InsufficientFundsForFee
                            or TransactionErrorType.InvalidRentPayingAccount;

                    bool isLamportError = createResult.ErrorData?.Logs?.Any(log =>
                        log.Contains("insufficient lamports", StringComparison.OrdinalIgnoreCase)) == true;

                    if (isBalanceError || isLamportError)
                        return HandleError<CreateCollectionNftResult>($"{createResult.Reason}.\n Insufficient SOL to cover the transaction fee or rent.");

                    return HandleError<CreateCollectionNftResult>(createResult.Reason);
                }

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
            finally
            {
                Console.SetOut(originalConsoleOut);
            }
        }
        catch (Exception ex)
        {
            return HandleError<CreateCollectionNftResult>(ex.Message);
        }
    }
}
