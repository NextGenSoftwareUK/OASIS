namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public sealed class SolanaService(Account oasisAccount, IRpcClient rpcClient) : ISolanaService
{
    private const uint SellerFeeBasisPoints = 500;
    private const byte CreatorShare = 100;
    private const string Solana = "Solana";

    private readonly List<Creator> _creators =
    [
        new(oasisAccount.PublicKey, share: CreatorShare, verified: true)
    ];


    public async Task<OASISResult<MintNftResult>> MintNftAsync(MintNFTTransactionRequestForProvider mintNftRequest)
    {
        try
        {
            MetadataClient metadataClient = new(rpcClient);
            Account mintAccount = new();

            Metadata tokenMetadata = new()
            {
                name = mintNftRequest.Title,
                symbol = mintNftRequest.Symbol,
                sellerFeeBasisPoints = SellerFeeBasisPoints,
                uri = mintNftRequest.JSONUrl,
                creators = _creators
            };

            RequestResult<string> createNftResult = await metadataClient.CreateNFT(
                ownerAccount: oasisAccount,
                mintAccount: mintAccount,
                TokenStandard.NonFungible,
                tokenMetadata,
                isMasterEdition: true,
                isMutable: true);

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
            return HandleError<MintNftResult>(ex.Message);
        }
    }

    public async Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest)
    {
        var response = new OASISResult<SendTransactionResult>();
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

        return response;
    }

    public async Task<OASISResult<GetNftResult>> LoadNftAsync(
        string address)
    {
        OASISResult<GetNftResult> response = new();
        try
        {
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

        return response;
    }


    public async Task<OASISResult<SendTransactionResult>> SendNftAsync(NFTWalletTransactionRequest mintNftRequest)
    {
        OASISResult<SendTransactionResult> response = new();
        try
        {
            RequestResult<ResponseValue<LatestBlockHash>> blockHash =
                await rpcClient.GetLatestBlockHashAsync();

            PublicKey receiverTokenAccount =
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                    new PublicKey(mintNftRequest.ToWalletAddress), new PublicKey(mintNftRequest.TokenAddress));
            PublicKey senderTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                new PublicKey(mintNftRequest.FromWalletAddress), new PublicKey(mintNftRequest.TokenAddress));

            RequestResult<ResponseValue<AccountInfo>> accountInfo =
                await rpcClient.GetAccountInfoAsync(receiverTokenAccount);
            if (accountInfo.Result == null)
            {
                var createAccountTransaction =
                    AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        new PublicKey(mintNftRequest.FromWalletAddress), new PublicKey(mintNftRequest.ToWalletAddress),
                        new PublicKey(mintNftRequest.TokenAddress));
                byte[] createAccountTransactionBytes = new TransactionBuilder()
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(new PublicKey(mintNftRequest.FromWalletAddress))
                    .AddInstruction(createAccountTransaction)
                    .Build(oasisAccount);
                await rpcClient.SendTransactionAsync(createAccountTransactionBytes);
            }

            TransactionInstruction transferTransaction = TokenProgram.Transfer(
                senderTokenAccount,
                receiverTokenAccount,
                (ulong)mintNftRequest.Amount,
                new PublicKey(mintNftRequest.FromWalletAddress)
            );

            byte[] transactionBytes = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(new PublicKey(mintNftRequest.FromWalletAddress))
                .AddInstruction(transferTransaction)
                .Build(oasisAccount);

            RequestResult<string> sendTransactionResult = await rpcClient.SendTransactionAsync(transactionBytes);

            if (!sendTransactionResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransactionResult.Reason;
                return response;
            }

            response.IsError = false;
            response.Result = new()
            {
                TransactionHash = sendTransactionResult.Result
            };
        }
        catch (Exception ex)
        {
            response.IsError = true;
            response.Message = ex.Message;
        }

        return response;
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
}