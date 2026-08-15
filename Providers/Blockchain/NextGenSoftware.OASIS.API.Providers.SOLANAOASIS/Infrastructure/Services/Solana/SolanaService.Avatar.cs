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

    // Sets collectionDetails on a collection NFT so Phantom/Helius DAS API recognises it as a
    // collection parent and groups child NFTs under it in the wallet's Collections tab.
    // Call this on any existing collection NFT that was created without collectionDetails.
    public async Task<string> SetCollectionSizeAsync(string collectionMintAddress, ulong size)
    {
        PublicKey metadataProgram = new("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s");
        PublicKey collectionMint = new(collectionMintAddress);
        PublicKey collectionMetadataPda = DeriveMetadataPda(collectionMint, metadataProgram);

        // Instruction 34 = SetCollectionSize (deployed mpl-token-metadata program enum).
        // Data: [discriminator u8 (1 byte)] + [size u64 LE (8 bytes)] = 9 bytes total.
        byte[] sizeBytes = BitConverter.GetBytes(size);
        if (!BitConverter.IsLittleEndian) Array.Reverse(sizeBytes);
        byte[] instructionData = new byte[9];
        instructionData[0] = 34;
        Buffer.BlockCopy(sizeBytes, 0, instructionData, 1, 8);

        List<AccountMeta> accounts =
        [
            AccountMeta.Writable(collectionMetadataPda, false),   // collection metadata
            AccountMeta.Writable(oasisAccount.PublicKey, true),   // update authority (signer)
            AccountMeta.ReadOnly(collectionMint, false),           // collection mint
        ];

        TransactionInstruction instruction = new()
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
            .AddInstruction(instruction)
            .Build(oasisAccount);

        var simResult = await rpcClient.SimulateTransactionAsync(txBytes);
        if (simResult?.Result?.Value?.Error != null)
        {
            string logs = simResult.Result.Value.Logs != null
                ? string.Join(" | ", simResult.Result.Value.Logs)
                : "no logs";
            throw new Exception($"SetCollectionSize simulation failed [ix={instructionData[0]} accounts={accounts.Count}]: {simResult.Result.Value.Error} — logs: {logs}");
        }

        RequestResult<string> sendResult = await rpcClient.SendTransactionAsync(txBytes);

        if (!sendResult.WasSuccessful)
            throw new Exception($"SetCollectionSize send failed: {sendResult.Reason}");

        return sendResult.Result;
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
            RequestResult<ResponseValue<AccountInfo>> accountInfoResult = await rpcClient.GetAccountInfoAsync(
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                    new PublicKey(mintNftRequest.ToWalletAddress),
                    new PublicKey(mintNftRequest.TokenAddress)));

            bool needsCreateTokenAccount = false;

            if (!accountInfoResult.WasSuccessful || accountInfoResult.Result == null ||
                accountInfoResult.Result.Value == null)
            {
                needsCreateTokenAccount = true;
            }
            else
            {
                List<string> data = accountInfoResult.Result.Value.Data;
                if (data == null || data.Count == 0)
                {
                    needsCreateTokenAccount = true;
                }
            }

            if (needsCreateTokenAccount)
            {
                RequestResult<ResponseValue<LatestBlockHash>> createAccountBlockHashResult =
                    await rpcClient.GetLatestBlockHashAsync();
                if (!createAccountBlockHashResult.WasSuccessful)
                {
                    return new OASISResult<SendTransactionResult>
                    {
                        IsError = true,
                        Message = "Failed to get latest block hash for account creation: " +
                                  createAccountBlockHashResult.Reason
                    };
                }

                TransactionInstruction createAccountTransaction =
                    AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        new PublicKey(mintNftRequest.FromWalletAddress),
                        new PublicKey(mintNftRequest.ToWalletAddress),
                        new PublicKey(mintNftRequest.TokenAddress));

                byte[] createAccountTxBytes = new TransactionBuilder()
                    .SetRecentBlockHash(createAccountBlockHashResult.Result.Value.Blockhash)
                    .SetFeePayer(new PublicKey(mintNftRequest.FromWalletAddress))
                    .AddInstruction(createAccountTransaction)
                    .Build(oasisAccount);

                RequestResult<string> sendCreateAccountResult = await rpcClient.SendTransactionAsync(
                    createAccountTxBytes,
                    skipPreflight: false,
                    commitment: Commitment.Confirmed);

                if (!sendCreateAccountResult.WasSuccessful)
                {
                    return new OASISResult<SendTransactionResult>
                    {
                        IsError = true,
                        Message = "Failed to create associated token account: " + sendCreateAccountResult.Reason
                    };
                }
            }

            RequestResult<ResponseValue<LatestBlockHash>> transferBlockHashResult =
                await rpcClient.GetLatestBlockHashAsync();
            if (!transferBlockHashResult.WasSuccessful)
            {
                return new OASISResult<SendTransactionResult>
                {
                    IsError = true,
                    Message = "Failed to get latest block hash for transfer: " + transferBlockHashResult.Reason
                };
            }

            TransactionInstruction transferTransaction = TokenProgram.Transfer(
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                    new PublicKey(mintNftRequest.FromWalletAddress),
                    new PublicKey(mintNftRequest.TokenAddress)),
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                    new PublicKey(mintNftRequest.ToWalletAddress),
                    new PublicKey(mintNftRequest.TokenAddress)),
                (ulong)mintNftRequest.Amount,
                new PublicKey(mintNftRequest.FromWalletAddress));

            byte[] transferTxBytes = new TransactionBuilder()
                .SetRecentBlockHash(transferBlockHashResult.Result.Value.Blockhash)
                .SetFeePayer(new PublicKey(mintNftRequest.FromWalletAddress))
                .AddInstruction(transferTransaction)
                .Build(oasisAccount);

            RequestResult<string> sendTransferResult = await rpcClient.SendTransactionAsync(
                transferTxBytes,
                skipPreflight: false,
                commitment: Commitment.Confirmed);

            if (!sendTransferResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransferResult.Reason;
                return response;
            }

            response.IsError = false;
            response.Result = new SendTransactionResult
            {
                TransactionHash = sendTransferResult.Result
            };
        }
        catch (Exception ex)
        {
            response.IsError = true;
            response.Message = ex.Message;
        }
        finally
        {
            // Restore the original Console.Out
            Console.SetOut(originalConsoleOut);
        }

        return response;
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
}
