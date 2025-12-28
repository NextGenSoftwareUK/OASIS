namespace SolanaBridge.Nft;

public interface ISolanaNftManager : INftManager;

public sealed class SolanaNftManager(
    INftWalletProvider walletProvider,
    INftMetadataSerializer metadataSerializer,
    MetadataClient metaplexClient,
    IRpcClient client) : ISolanaNftManager
{
    /// <inheritdoc />
    public async Task<Result<NftMintingResponse>> MintAsync(Common.DTOs.Nft nft, CancellationToken token = default)
    {
        Result<WalletKeyPair> walletResult = await walletProvider.GetWalletAsync(Networks.Solana, token);
        if (!walletResult.IsSuccess)
            return Result<NftMintingResponse>.Failure(walletResult.Error);

        WalletKeyPair walletKey = walletResult.Value!;
        Mnemonic mnemonic = new(Encoding.UTF8.GetString(walletKey.SeedPhrease));
        Wallet wallet = new(mnemonic);

        Account account = wallet.Account;
        Account mintAccount = new();

        Result<string> metadataResult = await metadataSerializer.SerializeAsync(nft, token);
        if (!metadataResult.IsSuccess)
            return Result<NftMintingResponse>.Failure(metadataResult.Error);

        string uri = metadataResult.Value!;

        List<Creator> creators =
        [
            new(account.PublicKey, share: 100, verified: true)
        ];

        Metadata metadata = new()
        {
            name = nft.Name,
            symbol = nft.Symbol,
            uri = uri,
            sellerFeeBasisPoints = ushort.TryParse(nft.Royality, out ushort fee) ? fee : (ushort)500,
            creators = creators
        };

        try
        {
            RequestResult<string> tx = await metaplexClient.CreateNFT(
                ownerAccount: account,
                mintAccount: mintAccount,
                tokenStandard: TokenStandard.NonFungible,
                metaData: metadata,
                isMasterEdition: true,
                isMutable: true
            );

            if (tx.WasSuccessful)
            {
                return Result<NftMintingResponse>.Success(
                    new(
                        mintAccount.PublicKey,
                        tx.Result,
                        uri,
                        Networks.Solana));
            }

            if (tx.ErrorData.Error.Type == TransactionErrorType.AccountNotFound)
                return Result<NftMintingResponse>.Failure(
                    ResultPatternError.BadRequest(Messages.AccountNotFound));

            return Result<NftMintingResponse>.Failure(
                ResultPatternError.InternalServerError(tx.Reason));
        }
        catch (Exception ex)
        {
            return Result<NftMintingResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Common.DTOs.Nft>> GetMetadataAsync(string nftAddress, CancellationToken token = default)
    {
        try
        {
            MetadataAccount account = await MetadataAccount.GetAccount(client, new PublicKey(nftAddress));

            Dictionary<string, string>? attributes = account.offchainData.attributes?
                .Where(attr => attr != null)
                .ToDictionary(x => x.trait_type, x => x.value);

            return Result<Common.DTOs.Nft>.Success(new Common.DTOs.Nft
            {
                Name = account.offchainData.name,
                Symbol = account.offchainData.symbol,
                Description = account.offchainData.description,
                ImageUrl = account.offchainData.default_image,
                Url = account.metadata.uri,
                Royality = account.metadata.sellerFeeBasisPoints.ToString(),
                AdditionalMetadata = attributes
            });
        }
        catch (Exception ex)
        {
            return Result<Common.DTOs.Nft>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<string>> BurnAsync(NftBurnRequest request, CancellationToken token = default)
    {
        try
        {
            Mnemonic mnemonic = new(request.OwnerSeedPhrase);
            Wallet wallet = new(mnemonic);
            Account owner = wallet.Account;

            PublicKey mintPublicKey = new(request.MintAddress);
            PublicKey associatedTokenAccount =
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner.PublicKey, mintPublicKey);

            RequestResult<ResponseValue<TokenBalance>> balanceResult =
                await client.GetTokenAccountBalanceAsync(associatedTokenAccount);
            if (balanceResult == null || balanceResult.Result?.Value == null || balanceResult.Result.Value.Amount == "0")
            {
                return Result<string>.Failure(ResultPatternError.BadRequest(balanceResult?.ErrorData?.ToString() ?? balanceResult?.Reason));
            }

            ulong amountToBurn = ulong.Parse(balanceResult.Result.Value.Amount);

            List<TransactionInstruction> instructions =
            [
                TokenProgram.Burn(
                    source: associatedTokenAccount,
                    mint: mintPublicKey,
                    amount: amountToBurn,
                    authority: owner.PublicKey
                ),

                TokenProgram.CloseAccount(
                    account: associatedTokenAccount,
                    destination: owner.PublicKey,
                    authority: owner.PublicKey,
                    programId: TokenProgram.ProgramIdKey
                )
            ];

            RequestResult<ResponseValue<LatestBlockHash>> blockHash = await client.GetLatestBlockHashAsync();
            if (blockHash.Result == null)
                return Result<string>.Failure(ResultPatternError.InternalServerError(blockHash.ErrorData.ToString()));

            TransactionBuilder txBuilder = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(owner.PublicKey);

            foreach (TransactionInstruction instruction in instructions)
            {
                txBuilder.AddInstruction(instruction);
            }

            byte[] tx = txBuilder.Build(new List<Account> { owner });

            RequestResult<string> txResult = await client.SendTransactionAsync(tx, commitment: Commitment.Confirmed);

            if (txResult.WasSuccessful)
                return Result<string>.Success(txResult.Result);

            return Result<string>.Failure(ResultPatternError.InternalServerError(txResult.ErrorData.ToString()));
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }
}