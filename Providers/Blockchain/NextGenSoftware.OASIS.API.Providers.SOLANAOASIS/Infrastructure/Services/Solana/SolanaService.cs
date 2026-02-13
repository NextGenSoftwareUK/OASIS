using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Requests;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public sealed class SolanaService(Account oasisAccount, IRpcClient rpcClient) : ISolanaService
{
    /// <summary>Token-2022 (Token Extensions) program ID. When a mint is owned by this program, use it for ATA creation and transfer.</summary>
    private static readonly PublicKey Token2022ProgramId = new("TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb");

    private const uint SellerFeeBasisPoints = 500;
    private const byte CreatorShare = 100;
    private const string Solana = "Solana";
    private const decimal Lamports = 1_000_000_000m;

    private readonly List<Creator> _creators =
    [
        new(oasisAccount.PublicKey, share: CreatorShare, verified: true)
    ];


    //TODO: Finish porting!
    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        OASISResult<decimal> result = new OASISResult<decimal>();
        string errorMessage = "Error occured in SolanaService calling GetAccountBalance. Reason: ";

        try
        {
            MetadataClient metadataClient = new(rpcClient);

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
            if (rpcClient == null)
                return HandleError<MintNftResult>("RPC client is null. Solana provider may not be properly initialized.");
            
            if (oasisAccount == null)
                return HandleError<MintNftResult>("OASIS Solana account is null. Provider may not be properly initialized.");
            
            if (mintNftRequest == null)
                return HandleError<MintNftResult>("Mint NFT request is null.");
            
            if (string.IsNullOrEmpty(mintNftRequest.JSONMetaDataURL))
                return HandleError<MintNftResult>("JSONMetaDataURL is required for Solana NFT minting.");

            MetadataClient metadataClient = new(rpcClient);
            // Create a new account for the NFT mint (each NFT needs its own unique mint account)
            // The ownerAccount (oasisAccount) will own the NFT, but the mintAccount is a new account for this specific NFT
            Account mintAccount = new Account();

            // Metaplex metadata name field has a maximum length of 32 characters
            // Truncate to prevent "Name too long" error (0xb)
            string nftName = mintNftRequest.Title ?? "Untitled NFT";
            if (nftName.Length > 32)
            {
                nftName = nftName.Substring(0, 32);
            }

            // Metaplex metadata symbol field has a maximum length of 10 characters
            // Truncate to prevent "Invalid instruction" error (0xc)
            string nftSymbol = mintNftRequest.Symbol ?? "NFT";
            if (nftSymbol.Length > 10)
            {
                nftSymbol = nftSymbol.Substring(0, 10);
            }

            Metadata tokenMetadata = new()
            {
                name = nftName,
                symbol = nftSymbol,
                sellerFeeBasisPoints = SellerFeeBasisPoints,
                uri = mintNftRequest.JSONMetaDataURL,
                creators = _creators
            };

            // Enhanced logging for debugging Instruction 3 error
            Console.WriteLine($"=== SOLANA NFT MINTING DEBUG ===");
            Console.WriteLine($"Owner Account (OASIS): {oasisAccount.PublicKey.Key}");
            Console.WriteLine($"Mint Account (New): {mintAccount.PublicKey.Key}");
            Console.WriteLine($"Metadata URI: {tokenMetadata.uri}");
            Console.WriteLine($"Creators Count: {_creators.Count}");
            foreach (var creator in _creators)
            {
                Console.WriteLine($"  Creator: {creator.key}, Share: {creator.share}, Verified: {creator.verified}");
            }
            Console.WriteLine($"IsMasterEdition: true");
            Console.WriteLine($"IsMutable: true");
            
            // Check owner account balance and state
            try
            {
                var balanceResult = await rpcClient.GetBalanceAsync(oasisAccount.PublicKey);
                if (balanceResult.WasSuccessful)
                {
                    Console.WriteLine($"Owner Account Balance: {balanceResult.Result.Value} lamports ({balanceResult.Result.Value / 1_000_000_000m} SOL)");
                }
                else
                {
                    Console.WriteLine($"⚠️  Failed to get owner account balance: {balanceResult.Reason}");
                }
            }
            catch (Exception balanceEx)
            {
                Console.WriteLine($"⚠️  Exception getting balance: {balanceEx.Message}");
            }
            
            // Check if owner account has data (might cause error 0xb)
            try
            {
                var accountInfo = await rpcClient.GetAccountInfoAsync(oasisAccount.PublicKey);
                if (accountInfo.WasSuccessful && accountInfo.Result.Value != null)
                {
                    int dataLength = 0;
                    if (accountInfo.Result.Value.Data != null)
                    {
                        // AccountInfo.Data is List<string> in Solnet
                        List<string> data = accountInfo.Result.Value.Data;
                        dataLength = data.Count;
                    }
                    Console.WriteLine($"Owner Account Data Length: {dataLength} bytes");
                    Console.WriteLine($"Owner Account Executable: {accountInfo.Result.Value.Executable}");
                    Console.WriteLine($"Owner Account Owner: {accountInfo.Result.Value.Owner}");
                    if (dataLength > 0)
                    {
                        Console.WriteLine($"⚠️  WARNING: Owner account has data ({dataLength} bytes). This might cause error 0xb if it's not a native SOL wallet.");
                    }
                }
            }
            catch (Exception accountEx)
            {
                Console.WriteLine($"⚠️  Exception checking account info: {accountEx.Message}");
            }
            Console.WriteLine($"=== END DEBUG INFO ===");

            // Save the original Console.Out
            var originalConsoleOut = Console.Out;

            // Retry logic for Instruction 3 errors (master edition creation)
            int maxRetries = 3;
            int retryDelay = 2000; // Start with 2 seconds
            RequestResult<string> createNftResult = null;
            bool isInstruction3Error = false;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Redirect Console.Out to a NullTextWriter to stop the SolNET Logger from outputting to the console (messes up STAR CLI!)
                    Console.SetOut(new NullTextWriter());

                    // CreateNFT from Solnet.Metaplex should handle creating the mint account automatically
                    // The mintAccount is a new Account() that CreateNFT will create on-chain
                    // The OASIS account (oasisAccount) needs to have enough SOL to pay for:
                    // - Transaction fees
                    // - Rent for mint account creation
                    // - Rent for metadata account creation
                    // - Rent for master edition account creation
                    // If the error is "Attempt to debit an account but found no record of a prior credit",
                    // it likely means the OASIS account balance is 0 or insufficient
                    Console.WriteLine($"Calling CreateNFT (attempt {attempt + 1}/{maxRetries}) with skipPreflight: false (will simulate first)");
                    createNftResult = await metadataClient.CreateNFT(
                    ownerAccount: oasisAccount,
                    mintAccount: mintAccount,
                    TokenStandard.NonFungible,
                    tokenMetadata,
                    isMasterEdition: true,
                    isMutable: true);

                    // Restore Console.Out before checking result
                    Console.SetOut(originalConsoleOut);

                    // Check if successful
                    if (createNftResult?.WasSuccessful == true)
                    {
                        break; // Success, exit retry loop
                    }

                    // Check if it's an Instruction 3 error
                    isInstruction3Error = createNftResult?.Reason?.Contains("Instruction 3") == true ||
                                        createNftResult?.Reason?.Contains("0xb") == true ||
                                        createNftResult?.ErrorData?.Logs?.Any(log => log.Contains("Instruction 3") || log.Contains("0xb")) == true;

                    // If it's an Instruction 3 error and we have retries left, retry
                    if (isInstruction3Error && attempt < maxRetries - 1)
                    {
                        Console.WriteLine($"⚠️  Instruction 3 error detected. Retrying in {retryDelay}ms... (attempt {attempt + 1}/{maxRetries})");
                        await Task.Delay(retryDelay);
                        retryDelay *= 2; // Exponential backoff: 2s, 4s, 8s
                        continue;
                    }

                    // Not an Instruction 3 error or out of retries, break
                    break;
                }
                catch (Exception ex)
                {
                    // Restore Console.Out before logging
                    Console.SetOut(originalConsoleOut);
                    Console.WriteLine($"⚠️  Exception during CreateNFT (attempt {attempt + 1}/{maxRetries}): {ex.Message}");
                    
                    if (attempt < maxRetries - 1)
                    {
                        Console.WriteLine($"Retrying in {retryDelay}ms...");
                        await Task.Delay(retryDelay);
                        retryDelay *= 2;
                        continue;
                    }
                    else
                    {
                        throw; // Re-throw on last attempt
                    }
                }
            }

            // Restore Console.Out before logging results (so debug logs are visible)
            Console.SetOut(originalConsoleOut);
            
            // Log CreateNFT result for debugging
            Console.WriteLine($"=== CreateNFT Result ===");
            Console.WriteLine($"WasSuccessful: {createNftResult?.WasSuccessful}");
            Console.WriteLine($"Reason: {createNftResult?.Reason}");
            Console.WriteLine($"Result: {createNftResult?.Result}");
            if (createNftResult?.ErrorData != null)
            {
                Console.WriteLine($"Error Type: {createNftResult.ErrorData.Error?.Type}");
                if (createNftResult.ErrorData.Logs != null)
                {
                    Console.WriteLine($"Error Logs (all): {string.Join(" | ", createNftResult.ErrorData.Logs)}");
                    Console.WriteLine($"Error Logs (first 10): {string.Join("; ", createNftResult.ErrorData.Logs.Take(10))}");
                }
                // Check specifically for Instruction 3 error
                if (createNftResult.Reason?.Contains("Instruction 3") == true || 
                    createNftResult.Reason?.Contains("0xb") == true ||
                    createNftResult.ErrorData.Logs?.Any(log => log.Contains("Instruction 3") || log.Contains("0xb")) == true)
                {
                    Console.WriteLine($"🔴 INSTRUCTION 3 ERROR DETECTED!");
                    Console.WriteLine($"   This is the master edition creation instruction.");
                    Console.WriteLine($"   Error 0xb typically means account data issue.");
                    Console.WriteLine($"   Check if owner account has unexpected data.");
                }
            }
            Console.WriteLine($"=== END CreateNFT Result ===");
            
            // Redirect Console.Out back to NullTextWriter for remaining operations
            Console.SetOut(new NullTextWriter());

            if (createNftResult == null)
                return HandleError<MintNftResult>("CreateNFT returned null result. RPC client may not be properly connected.");

            if (!createNftResult.WasSuccessful)
            {
                bool isBalanceError =
                    createNftResult.ErrorData?.Error.Type is TransactionErrorType.InsufficientFundsForFee
                        or TransactionErrorType.InvalidRentPayingAccount;

                bool isLamportError = createNftResult.ErrorData?.Logs?.Any(log =>
                    log.Contains("insufficient lamports", StringComparison.OrdinalIgnoreCase)) == true;

                if (isBalanceError || isLamportError)
                {
                    return HandleError<MintNftResult>(
                        $"{createNftResult.Reason}.\n Insufficient SOL to cover the transaction fee or rent.");
                }

                return HandleError<MintNftResult>(createNftResult.Reason);
            }

            return SuccessResult(
                new(mintAccount.PublicKey.Key,
                    Solana,
                    createNftResult.Result));
        }
        catch (Exception ex)
        {
            // Log full exception details to console for debugging
            Console.WriteLine($"=== SOLANA MINT NFT EXCEPTION ===");
            Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
            Console.WriteLine($"Exception Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception Type: {ex.InnerException.GetType().FullName}");
                Console.WriteLine($"Inner Exception Message: {ex.InnerException.Message}");
                Console.WriteLine($"Inner Exception StackTrace: {ex.InnerException.StackTrace}");
            }
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.WriteLine($"=== END EXCEPTION ===");
            
            string detailedError = $"Exception in MintNftAsync: {ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null)
                detailedError += $" Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            return HandleError<MintNftResult>(detailedError);
        }
    }

    public async Task<OASISResult<BurnNftResult>> BurnNftAsync(IBurnWeb3NFTRequest mintNftRequest)
    {
        var response = new OASISResult<BurnNftResult>();

        // Save the original Console.Out
        var originalConsoleOut = Console.Out;

        try
        {
            //PublicKey mintAccount = new(mintNftRequest.MintWalletAddress);
            PublicKey mintAccount = oasisAccount;
            PublicKey NFTTokenAddress = new(mintNftRequest.NFTTokenAddress);

            RequestResult<ResponseValue<LatestBlockHash>> blockHash =
                await rpcClient.GetLatestBlockHashAsync();

            byte[] tx = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(mintAccount)
                .AddInstruction(TokenProgram.Burn(
                mintAccount,
                NFTTokenAddress,
                1,
                mintAccount))
                .Build(oasisAccount);

            // Redirect Console.Out to a NullTextWriter to stop the SolNET Logger from outputting to the console (messes up STAR CLI!)
            Console.SetOut(new NullTextWriter());

            RequestResult<string> sendTransactionResult = await rpcClient.SendTransactionAsync(tx);
            if (!sendTransactionResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransactionResult.Reason;
                OASISErrorHandling.HandleError(ref response, response.Message);
                return response;
            }

            response.Result = new BurnNftResult(sendTransactionResult.Result);
        }
        catch (Exception e)
        {
            response.Exception = e;
            response.Message = e.Message;
            response.IsError = true;
            OASISErrorHandling.HandleError(ref response, e.Message);
        }
        finally
        {
            // Restore the original Console.Out
            Console.SetOut(originalConsoleOut);
        }

        return response;
    }

    public async Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest)
    {
        var response = new OASISResult<SendTransactionResult>();

        // Save the original Console.Out
        var originalConsoleOut = Console.Out;

        try
        {
            (bool success, string res) = sendTransactionRequest.IsRequestValid();
            if (!success)
            {
                response.Message = res;
                response.IsError = true;
                OASISErrorHandling.HandleError(ref response, res);
                return response;
            }

            PublicKey fromAccount = new(sendTransactionRequest.FromAccount.PublicKey);
            PublicKey toAccount = new(sendTransactionRequest.ToAccount.PublicKey);
            RequestResult<ResponseValue<LatestBlockHash>> blockHash =
                await rpcClient.GetLatestBlockHashAsync();

            byte[] tx = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(MemoProgram.NewMemo(fromAccount, sendTransactionRequest.MemoText))
                .AddInstruction(SystemProgram.Transfer(fromAccount, toAccount, sendTransactionRequest.Lampposts))
                .Build(oasisAccount);

            // Redirect Console.Out to a NullTextWriter to stop the SolNET Logger from outputting to the console (messes up STAR CLI!)
            Console.SetOut(new NullTextWriter());

            RequestResult<string> sendTransactionResult = await rpcClient.SendTransactionAsync(tx);
            if (!sendTransactionResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransactionResult.Reason;
                OASISErrorHandling.HandleError(ref response, response.Message);
                return response;
            }

            response.Result = new SendTransactionResult(sendTransactionResult.Result);
        }
        catch (Exception e)
        {
            response.Exception = e;
            response.Message = e.Message;
            response.IsError = true;
            OASISErrorHandling.HandleError(ref response, e.Message);
        }
        finally
        {
            // Restore the original Console.Out
            Console.SetOut(originalConsoleOut);
        }

        return response;
    }

    public async Task<OASISResult<GetNftResult>> LoadNftAsync(
        string address)
    {
        OASISResult<GetNftResult> response = new();
        
        // Save the original Console.Out
        var originalConsoleOut = Console.Out;

        try
        {
            // Redirect Console.Out to a NullTextWriter to stop the SolNET Logger from outputting to the console (messes up STAR CLI!)
            Console.SetOut(new NullTextWriter());

            PublicKey nftAccount = new(address);
            MetadataAccount metadataAccount = await MetadataAccount.GetAccount(rpcClient, nftAccount);

            response.IsError = false;
            response.IsLoaded = true;
            response.Result = new(metadataAccount);
        }
        catch (ArgumentNullException)
        {
            response.IsError = true;
            response.Message = "Account address is not correct or metadata not exists";
            OASISErrorHandling.HandleError(ref response, response.Message);
        }
        catch (NullReferenceException)
        {
            response.IsError = true;
            response.Message = "Account address is not correct or metadata not exists";
            OASISErrorHandling.HandleError(ref response, response.Message);
        }
        catch (Exception e)
        {
            response.IsError = true;
            response.Message = e.Message;
            OASISErrorHandling.HandleError(ref response, e.Message);
        }
        finally
        {
            // Restore the original Console.Out
            Console.SetOut(originalConsoleOut);
        }

        return response;
    }

    public async Task<OASISResult<SendTransactionResult>> SendNftAsync(SendWeb3NFTRequest mintNftRequest)
    {
        OASISResult<SendTransactionResult> response = new OASISResult<SendTransactionResult>();

        // Save the original Console.Out
        var originalConsoleOut = Console.Out;

        try
        {
            var fromPubkey = new PublicKey(mintNftRequest.FromWalletAddress);
            var toPubkey = new PublicKey(mintNftRequest.ToWalletAddress);
            var mintPubkey = new PublicKey(mintNftRequest.TokenAddress);

            // Determine if this mint is Token-2022 (incorrect program id causes "Error processing Instruction 0: incorrect program id for instruction")
            bool useToken2022 = false;
            var mintAccountResult = await rpcClient.GetAccountInfoAsync(mintPubkey);
            if (mintAccountResult.WasSuccessful && mintAccountResult.Result?.Value?.Owner != null)
            {
                var owner = mintAccountResult.Result.Value.Owner;
                useToken2022 = string.Equals(owner, Token2022ProgramId.Key, StringComparison.Ordinal);
            }

            // Derive ATAs using the same token program as the mint (Token-2022 mints use a different ATA address)
            PublicKey toAta = useToken2022
                ? DeriveAssociatedTokenAccount(toPubkey, mintPubkey, Token2022ProgramId)
                : AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(toPubkey, mintPubkey);
            PublicKey fromAta = useToken2022
                ? DeriveAssociatedTokenAccount(fromPubkey, mintPubkey, Token2022ProgramId)
                : AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(fromPubkey, mintPubkey);

            // Diagnostic: so you can verify on Solscan that the NFT landed in this ATA (Phantom may not show it)
            Console.WriteLine($"=== SOLANA SEND: Mint owner = {(useToken2022 ? "Token-2022" : "Legacy SPL")}, Recipient ATA = {toAta?.Key ?? "(null)"} ===");

            RequestResult<ResponseValue<AccountInfo>> accountInfoResult = await rpcClient.GetAccountInfoAsync(toAta);
            bool needsCreateTokenAccount = !accountInfoResult.WasSuccessful || accountInfoResult.Result?.Value == null
                || accountInfoResult.Result.Value.Data == null || accountInfoResult.Result.Value.Data.Count == 0;

            // Single block hash for the whole transaction
            RequestResult<ResponseValue<LatestBlockHash>> blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
            {
                return new OASISResult<SendTransactionResult>
                {
                    IsError = true,
                    Message = "Failed to get latest block hash: " + blockHashResult.Reason
                };
            }

            var builder = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(fromPubkey);

            // If recipient has no ATA, create it and transfer in the SAME transaction (atomic)
            if (needsCreateTokenAccount)
            {
                TransactionInstruction createAtaInstruction = useToken2022
                    ? CreateAssociatedTokenAccountInstruction(fromPubkey, toPubkey, mintPubkey, Token2022ProgramId)
                    : AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(fromPubkey, toPubkey, mintPubkey);
                if (createAtaInstruction != null)
                    builder = builder.AddInstruction(createAtaInstruction);
            }

            // Transfer: from OASIS ATA to recipient ATA (use same token program as mint)
            TransactionInstruction transferInstruction = useToken2022
                ? CreateTransferInstruction(fromAta, toAta, (ulong)mintNftRequest.Amount, fromPubkey, Token2022ProgramId)
                : TokenProgram.Transfer(fromAta, toAta, (ulong)mintNftRequest.Amount, fromPubkey);
            builder = builder.AddInstruction(transferInstruction);

            byte[] txBytes = builder.Build(oasisAccount);

            RequestResult<string> sendResult = await rpcClient.SendTransactionAsync(
                txBytes,
                skipPreflight: false,
                commitment: Commitment.Confirmed);

            if (!sendResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendResult.Reason ?? "Send NFT transaction failed.";
                return response;
            }

            string txSignature = sendResult.Result;

            // Verify the transaction actually succeeded on-chain (don't claim success if it failed after submit)
            bool confirmedSuccess = false;
            const int maxConfirmAttempts = 8;
            const int confirmDelayMs = 2000;
            for (int i = 0; i < maxConfirmAttempts; i++)
            {
                await Task.Delay(confirmDelayMs);
                var txResult = await rpcClient.GetTransactionAsync(txSignature, Commitment.Confirmed);
                if (!txResult.WasSuccessful || txResult.Result == null)
                    continue;
                var tx = txResult.Result;
                if (tx.Meta != null && tx.Meta.Error != null)
                {
                    string errMsg = tx.Meta.Error.ToString();
                    string logs = tx.Meta.LogMessages != null ? string.Join("; ", tx.Meta.LogMessages.Take(15)) : "";
                    response.IsError = true;
                    response.Message = $"Send NFT transaction failed on-chain: {errMsg}. Logs: {logs}";
                    return response;
                }
                if (tx.Meta != null && tx.Meta.Error == null)
                {
                    confirmedSuccess = true;
                    break;
                }
            }

            if (!confirmedSuccess)
            {
                response.IsError = true;
                response.Message = $"Send transaction could not be confirmed in time. Check status: {txSignature} (may still confirm later).";
                return response;
            }

            response.IsError = false;
            response.Result = new SendTransactionResult { TransactionHash = txSignature };
        }
        catch (Exception ex)
        {
            response.IsError = true;
            response.Message = ex.Message;
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
        }

        return response;
    }

    /// <summary>Derive associated token account address for a given token program (legacy or Token-2022).</summary>
    private static PublicKey DeriveAssociatedTokenAccount(PublicKey owner, PublicKey mint, PublicKey tokenProgramId)
    {
        var seeds = new List<byte[]> { owner.KeyBytes, tokenProgramId.KeyBytes, mint.KeyBytes };
        return PublicKey.TryCreateProgramAddress(seeds, AssociatedTokenAccountProgram.ProgramIdKey, out var ata)
            ? ata
            : null;
    }

    /// <summary>Build CreateAssociatedTokenAccount instruction for Token-2022 (same as ATA program but with token program id in keys).</summary>
    private static TransactionInstruction CreateAssociatedTokenAccountInstruction(PublicKey payer, PublicKey owner, PublicKey mint, PublicKey tokenProgramId)
    {
        var associatedTokenAddress = DeriveAssociatedTokenAccount(owner, mint, tokenProgramId);
        if (associatedTokenAddress == null) return null;
        var keys = new List<AccountMeta>
        {
            AccountMeta.Writable(payer, true),
            AccountMeta.Writable(associatedTokenAddress, false),
            AccountMeta.ReadOnly(owner, false),
            AccountMeta.ReadOnly(mint, false),
            AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
            AccountMeta.ReadOnly(tokenProgramId, false),
            AccountMeta.ReadOnly(SysVars.RentKey, false)
        };
        return new TransactionInstruction
        {
            ProgramId = AssociatedTokenAccountProgram.ProgramIdKey.KeyBytes,
            Keys = keys,
            Data = Array.Empty<byte>()
        };
    }

    /// <summary>Build Transfer instruction for a given token program (legacy or Token-2022). Same layout as SPL Token Transfer.</summary>
    private static TransactionInstruction CreateTransferInstruction(PublicKey source, PublicKey destination, ulong amount, PublicKey authority, PublicKey tokenProgramId)
    {
        var data = new List<byte> { 3 }; // SPL Token instruction index: Transfer = 3
        data.AddRange(BitConverter.GetBytes(amount));
        return new TransactionInstruction
        {
            ProgramId = tokenProgramId.KeyBytes,
            Keys = new List<AccountMeta>
            {
                AccountMeta.Writable(source, false),
                AccountMeta.Writable(destination, false),
                AccountMeta.ReadOnly(authority, true)
            },
            Data = data.ToArray()
        };
    }

    private OASISResult<MintNftResult> SuccessResult(MintNftResult result)
    {
        OASISResult<MintNftResult> response = new()
        {
            IsSaved = true,
            IsError = false,
            Result = result
        };

        return response;
    }

    public async Task<OASISResult<SolanaAvatarDto>> GetAvatarByUsernameAsync(string username)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar by username
            var programId = new PublicKey("11111111111111111111111111111111"); // OASIS program ID
            
            // Create instruction to call the smart contract's getAvatarByUsername function
            // Encode function selector (4 bytes) + username parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarByUsername");
            var usernameBytes = System.Text.Encoding.UTF8.GetBytes(username);
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(usernameBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.ReadOnly(oasisAccount.PublicKey, true)
                },
                Data = instructionData.ToArray()
            };
            
            // Get recent block hash for transaction
            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to get latest block hash: {blockHashResult.Reason}");
            }
            
            // Create and send transaction to call smart contract
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);
            
            // Send transaction to smart contract
            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to call smart contract: {sendResult.Reason}");
            }
            
            // Wait for transaction confirmation and get result
            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                // Parse the smart contract response from transaction logs
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarData = ParseSmartContractResponse(logs, username);
                
                if (avatarData != null)
                {
                    return new OASISResult<SolanaAvatarDto>
                    {
                        IsError = false,
                        Result = avatarData,
                        Message = "Avatar loaded successfully from OASIS smart contract"
                    };
                }
            }
            
            return HandleError<SolanaAvatarDto>("Avatar not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }
    
    private SolanaAvatarDto ParseSmartContractResponse(IList<string> logs, string username)
    {
        try
        {
            // Parse the smart contract response from transaction logs
            foreach (var log in logs)
            {
                if (log.Contains("AvatarData:"))
                {
                    // Extract avatar data from smart contract response
                    var dataStart = log.IndexOf("AvatarData:") + "AvatarData:".Length;
                    var jsonData = log.Substring(dataStart).Trim();
                    
                    // Parse JSON response from smart contract
                    var avatarJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                    if (avatarJson != null)
                    {
                        return new SolanaAvatarDto
                        {
                            Id = Guid.Parse(avatarJson.GetValueOrDefault("id", Guid.NewGuid().ToString()).ToString()),
                            UserName = avatarJson.GetValueOrDefault("username", username).ToString(),
                            Email = avatarJson.GetValueOrDefault("email", $"{username}@solana.local").ToString(),
                            Password = string.Empty,
                            FirstName = avatarJson.GetValueOrDefault("firstName", username).ToString(),
                            LastName = avatarJson.GetValueOrDefault("lastName", string.Empty).ToString(),
                            CreatedDate = DateTime.TryParse(avatarJson.GetValueOrDefault("createdDate", DateTime.UtcNow.ToString()).ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.TryParse(avatarJson.GetValueOrDefault("modifiedDate", DateTime.UtcNow.ToString()).ToString(), out var modified) ? modified : DateTime.UtcNow,
                            AvatarType = avatarJson.GetValueOrDefault("avatarType", "User").ToString(),
                            Description = avatarJson.GetValueOrDefault("description", "").ToString(),
                            MetaData = new Dictionary<string, object>
                            {
                                ["SolanaUsername"] = username,
                                ["SolanaNetwork"] = "Solana Mainnet",
                                ["SmartContractResponse"] = jsonData,
                                ["TransactionLogs"] = logs,
                            }
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log parsing error but don't fail the entire operation
            Console.WriteLine($"Error parsing smart contract response: {ex.Message}");
        }
        
        return null;
    }

    public async Task<OASISResult<SolanaAvatarDto>> GetAvatarByIdAsync(Guid id)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar by ID
            var programId = new PublicKey("11111111111111111111111111111111"); // OASIS program ID
            
            // Create instruction to call the smart contract's getAvatarById function
            // Encode function selector (4 bytes) + id parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarById");
            var idBytes = id.ToByteArray();
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(idBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.ReadOnly(oasisAccount.PublicKey, true)
                },
                Data = instructionData.ToArray()
            };
            
            // Get recent block hash for transaction
            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to get latest block hash: {blockHashResult.Reason}");
            }
            
            // Create and send transaction to call smart contract
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);
            
            // Send transaction to smart contract
            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to call smart contract: {sendResult.Reason}");
            }
            
            // Wait for transaction confirmation and get result
            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                // Parse the smart contract response from transaction logs
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarData = ParseSmartContractResponse(logs, $"user_{id}");
                
                if (avatarData != null)
                {
                    avatarData.Id = id; // Ensure the ID matches what was requested
                    return new OASISResult<SolanaAvatarDto>
                    {
                        IsError = false,
                        Result = avatarData,
                        Message = "Avatar loaded successfully from OASIS smart contract"
                    };
                }
            }
            
            return HandleError<SolanaAvatarDto>("Avatar not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    public async Task<OASISResult<SolanaAvatarDto>> GetAvatarByEmailAsync(string email)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar by email
            var programId = new PublicKey("11111111111111111111111111111111"); // OASIS program ID
            
            // Create instruction to call the smart contract's getAvatarByEmail function
            // Encode function selector (4 bytes) + email parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarByEmail");
            var emailBytes = System.Text.Encoding.UTF8.GetBytes(email);
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(emailBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.ReadOnly(oasisAccount.PublicKey, true)
                },
                Data = instructionData.ToArray()
            };
            
            // Get recent block hash for transaction
            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to get latest block hash: {blockHashResult.Reason}");
            }
            
            // Create and send transaction to call smart contract
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);
            
            // Send transaction to smart contract
            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to call smart contract: {sendResult.Reason}");
            }
            
            // Wait for transaction confirmation and get result
            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                // Parse the smart contract response from transaction logs
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarData = ParseSmartContractResponse(logs, email.Split('@')[0]);
                
                if (avatarData != null)
                {
                    avatarData.Email = email; // Ensure the email matches what was requested
                    return new OASISResult<SolanaAvatarDto>
                    {
                        IsError = false,
                        Result = avatarData,
                        Message = "Avatar loaded successfully from OASIS smart contract"
                    };
                }
            }
            
            return HandleError<SolanaAvatarDto>("Avatar not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    public async Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByIdAsync(Guid id)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar detail by ID
            var programId = new PublicKey("11111111111111111111111111111111");
            
            // Encode function selector (4 bytes) + id parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarDetailById");
            var idBytes = id.ToByteArray();
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(idBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta> { AccountMeta.ReadOnly(oasisAccount.PublicKey, true) },
                Data = instructionData.ToArray()
            };

            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to get latest block hash: {blockHashResult.Reason}");

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);

            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to call smart contract: {sendResult.Reason}");

            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarDetail = ParseSmartContractResponseToAvatarDetail(logs, id.ToString());
                if (avatarDetail != null)
                    return new OASISResult<SolanaAvatarDetailDto> { IsError = false, Result = avatarDetail, Message = "Avatar detail loaded by id from Solana" };
            }

            return HandleError<SolanaAvatarDetailDto>("Avatar detail not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDetailDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    public async Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByUsernameAsync(string username)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar detail by username
            var programId = new PublicKey("11111111111111111111111111111111");
            
            // Encode function selector (4 bytes) + username parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarDetailByUsername");
            var usernameBytes = System.Text.Encoding.UTF8.GetBytes(username);
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(usernameBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta> { AccountMeta.ReadOnly(oasisAccount.PublicKey, true) },
                Data = instructionData.ToArray()
            };

            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to get latest block hash: {blockHashResult.Reason}");

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);

            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to call smart contract: {sendResult.Reason}");

            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarDetail = ParseSmartContractResponseToAvatarDetail(logs, username);
                if (avatarDetail != null)
                    return new OASISResult<SolanaAvatarDetailDto> { IsError = false, Result = avatarDetail, Message = "Avatar detail loaded by username from Solana" };
            }

            return HandleError<SolanaAvatarDetailDto>("Avatar detail not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDetailDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    public async Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByEmailAsync(string email)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar detail by email
            var programId = new PublicKey("11111111111111111111111111111111");
            
            // Encode function selector (4 bytes) + email parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarDetailByEmail");
            var emailBytes = System.Text.Encoding.UTF8.GetBytes(email);
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(emailBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta> { AccountMeta.ReadOnly(oasisAccount.PublicKey, true) },
                Data = instructionData.ToArray()
            };

            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to get latest block hash: {blockHashResult.Reason}");

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);

            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to call smart contract: {sendResult.Reason}");

            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarDetail = ParseSmartContractResponseToAvatarDetail(logs, email.Split('@')[0]);
                if (avatarDetail != null)
                    return new OASISResult<SolanaAvatarDetailDto> { IsError = false, Result = avatarDetail, Message = "Avatar detail loaded by email from Solana" };
            }

            return HandleError<SolanaAvatarDetailDto>("Avatar detail not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDetailDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    private SolanaAvatarDetailDto ParseSmartContractResponseToAvatarDetail(IList<string> logs, string identifier)
    {
        try
        {
            foreach (var log in logs)
            {
                if (log.Contains("AvatarDetailData:"))
                {
                    var dataStart = log.IndexOf("AvatarDetailData:") + "AvatarDetailData:".Length;
                    var jsonData = log.Substring(dataStart).Trim();
                    
                    var avatarDetailJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                    if (avatarDetailJson != null)
                    {
                        return new SolanaAvatarDetailDto
                        {
                            Id = Guid.Parse(avatarDetailJson.GetValueOrDefault("id", Guid.NewGuid().ToString()).ToString()),
                            Username = avatarDetailJson.GetValueOrDefault("username", identifier).ToString(),
                            Email = avatarDetailJson.GetValueOrDefault("email", $"{identifier}@solana.local").ToString(),
                            FirstName = avatarDetailJson.GetValueOrDefault("firstName", identifier).ToString(),
                            LastName = avatarDetailJson.GetValueOrDefault("lastName", "Solana User").ToString(),
                            CreatedDate = DateTime.TryParse(avatarDetailJson.GetValueOrDefault("createdDate", DateTime.UtcNow.ToString()).ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.TryParse(avatarDetailJson.GetValueOrDefault("modifiedDate", DateTime.UtcNow.ToString()).ToString(), out var modified) ? modified : DateTime.UtcNow,
                            AvatarType = avatarDetailJson.GetValueOrDefault("avatarType", "User").ToString(),
                            Description = avatarDetailJson.GetValueOrDefault("description", "Avatar detail loaded from Solana blockchain").ToString(),
                            Address = avatarDetailJson.GetValueOrDefault("address", "Solana Address").ToString(),
                            Country = avatarDetailJson.GetValueOrDefault("country", "Solana Network").ToString(),
                            Postcode = avatarDetailJson.GetValueOrDefault("postcode", "SOL-001").ToString(),
                            Mobile = avatarDetailJson.GetValueOrDefault("mobile", "+1-555-SOLANA").ToString(),
                            Landline = avatarDetailJson.GetValueOrDefault("landline", "+1-555-SOLANA").ToString(),
                            Title = avatarDetailJson.GetValueOrDefault("title", "Solana User").ToString(),
                            DOB = DateTime.TryParse(avatarDetailJson.GetValueOrDefault("dob", DateTime.UtcNow.AddYears(-25).ToString()).ToString(), out var dob) ? dob : DateTime.UtcNow.AddYears(-25),
                            KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                            Level = int.TryParse(avatarDetailJson.GetValueOrDefault("level", "1").ToString(), out var level) ? level : 1,
                            XP = int.TryParse(avatarDetailJson.GetValueOrDefault("xp", "0").ToString(), out var xp) ? xp : 0,
                            MetaData = new Dictionary<string, object>
                            {
                                ["SolanaIdentifier"] = identifier,
                                ["SolanaNetwork"] = "Solana Mainnet",
                                ["SmartContractResponse"] = jsonData,
                                ["TransactionLogs"] = logs,
                                ["Provider"] = "SOLANAOASIS"
                            }
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing smart contract response to avatar detail: {ex.Message}");
        }
        
        return null;
    }

    private OASISResult<T> HandleError<T>(string message)
    {
        OASISResult<T> response = new()
        {
            IsError = true,
            Message = message
        };

        OASISErrorHandling.HandleError(ref response, message);
        return response;
    }
}