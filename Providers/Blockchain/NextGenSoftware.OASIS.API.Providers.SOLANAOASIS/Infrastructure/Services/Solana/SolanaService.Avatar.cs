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
        if(simResult==null || !simResult.WasSuccessful || simResult.Result?.Value==null)
            throw new InvalidOperationException("SetCollectionSize simulation result is unavailable; no transaction submitted.");
        if (simResult.Result.Value.Error != null)
        {
            string logs = simResult.Result.Value.Logs != null
                ? string.Join(" | ", simResult.Result.Value.Logs)
                : "no logs";
            throw new Exception($"SetCollectionSize simulation failed [ix={instructionData[0]} accounts={accounts.Count}]: {simResult.Result.Value.Error} — logs: {logs}");
        }

        return await new NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories.SolanaRepository(
            oasisAccount,rpcClient).SubmitSignedTransactionAsync(txBytes);
    }

    public async Task<OASISResult<BurnNftResult>> BurnNftAsync(IBurnWeb3NFTRequest mintNftRequest)
    {
        var response = new OASISResult<BurnNftResult>();

        try
        {
            ArgumentNullException.ThrowIfNull(mintNftRequest);
            var mint=new PublicKey(mintNftRequest.NFTTokenAddress);
            if(mint.KeyBytes.Length!=32) throw new ArgumentException("A valid NFT mint address is required.");
            var signer=Infrastructure.SolanaSigningIdentity.ResolveNftOwner(mintNftRequest.OwnerPrivateKey,
                mintNftRequest.OwnerPublicKey,mintNftRequest.OwnerSeedPhrase,oasisAccount);
            var metadataProgram=MetadataProgram.ProgramIdKey;
            var metadataPda=DeriveMetadataPda(mint,metadataProgram);
            var edition=DeriveMasterEditionPda(mint,metadataProgram);
            var metadataReply=await rpcClient.GetAccountInfoAsync(metadataPda.Key,Solnet.Rpc.Types.Commitment.Finalized);
            if(!metadataReply.WasSuccessful || metadataReply.Result?.Value==null || metadataReply.Result.Value.Owner!=metadataProgram.Key)
                throw new InvalidOperationException("NFT metadata is missing or not owned by the Metaplex program.");
            var rawMetadata=metadataReply.Result.Value;
            if(rawMetadata.Executable || rawMetadata.Data?.Count!=2 || rawMetadata.Data[1]!="base64")
                throw new System.IO.InvalidDataException("Invalid Metaplex metadata account encoding.");
            var verifiedCollection=MetaplexBurnMetadata.ReadVerifiedCollection(Convert.FromBase64String(rawMetadata.Data[0]),mint);
            var token=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(signer.PublicKey,mint);
            // Official mpl-token-metadata BurnNft ABI: discriminator29 and six required accounts.
            var accounts=new List<AccountMeta> {AccountMeta.Writable(metadataPda,false),AccountMeta.Writable(signer.PublicKey,true),
                AccountMeta.Writable(mint,false),AccountMeta.Writable(token,false),AccountMeta.Writable(edition,false),AccountMeta.ReadOnly(TokenProgram.ProgramIdKey,false)};
            if(verifiedCollection!=null)
                accounts.Add(AccountMeta.Writable(DeriveMetadataPda(verifiedCollection,metadataProgram),false));
            var instruction=new TransactionInstruction {ProgramId=metadataProgram.KeyBytes,Keys=accounts,Data=new byte[] {29}};
            var blockHash=await rpcClient.GetLatestBlockHashAsync();
            if(!blockHash.WasSuccessful || blockHash.Result?.Value==null) throw new InvalidOperationException("NFT burn blockhash lookup failed.");
            var tx=new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).SetFeePayer(signer).AddInstruction(instruction).Build(signer);
            var simulation=await rpcClient.SimulateTransactionAsync(tx);
            if(simulation==null || !simulation.WasSuccessful || simulation.Result?.Value==null || simulation.Result.Value.Error!=null)
                throw new InvalidOperationException("NFT burn simulation failed; no transaction submitted: "+simulation?.Result?.Value?.Error);
            var signature=await new NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories.SolanaRepository(signer,rpcClient).SubmitSignedTransactionAsync(tx);
            response.Result = new BurnNftResult(signature);
            response.IsSaved=true;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref response, e.Message,e);
        }

        return response;
    }

    /// <summary>
    /// Sends SOL (with a memo) from <c>FromAccount</c>. The transaction is signed by <paramref name="signerAccount"/>
    /// when given (e.g. an agent's own wallet), otherwise by the OASIS account. The signer must be
    /// <c>FromAccount</c>: Solana rejects a transfer whose fee payer/sender didn't sign it.
    /// </summary>
    public async Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest, Account signerAccount = null)
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

            // The fee payer and sender must sign. Previously this always signed with the OASIS account, so any
            // FromAccount other than the OASIS account failed signature verification on-chain.
            Account signer = signerAccount ?? oasisAccount;
            if (signer.PublicKey.Key != fromAccount.Key)
            {
                string mismatch = $"FromAccount {fromAccount.Key} must match the signing account {signer.PublicKey.Key}. " +
                    "Pass that account's signer to send from it.";
                OASISErrorHandling.HandleError(ref response, mismatch);
                return response;
            }

            RequestResult<ResponseValue<LatestBlockHash>> blockHash =
                await rpcClient.GetLatestBlockHashAsync();

            byte[] tx = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(MemoProgram.NewMemo(fromAccount, sendTransactionRequest.MemoText))
                .AddInstruction(SystemProgram.Transfer(fromAccount, toAccount, sendTransactionRequest.Lampposts))
                //.Build(oasisAccount);
                .Build(signer);

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

    public async Task<OASISResult<SendTransactionResult>> SendNftAsync(SendWeb3NFTRequest mintNftRequest)
    {
        OASISResult<SendTransactionResult> response = new OASISResult<SendTransactionResult>();
        try
        {
            ArgumentNullException.ThrowIfNull(mintNftRequest);
            if(mintNftRequest.Amount!=1) throw new ArgumentException("An NFT transfer must send exactly one token.");
            var sender=new PublicKey(mintNftRequest.FromWalletAddress);var recipient=new PublicKey(mintNftRequest.ToWalletAddress);
            var mint=new PublicKey(mintNftRequest.TokenAddress);
            if(sender.KeyBytes.Length!=32 || recipient.KeyBytes.Length!=32 || mint.KeyBytes.Length!=32) throw new ArgumentException("Valid native transfer addresses are required.");
            var signer=Infrastructure.SolanaSigningIdentity.ResolveNftOwner(mintNftRequest.OwnerPrivateKey,
                string.IsNullOrWhiteSpace(mintNftRequest.OwnerPublicKey) ? sender.Key:mintNftRequest.OwnerPublicKey,mintNftRequest.OwnerSeedPhrase,oasisAccount);
            if(signer.PublicKey.Key!=sender.Key) throw new ArgumentException("NFT transfer signer does not match sender.");
            var source=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(sender,mint);
            if(!string.IsNullOrWhiteSpace(mintNftRequest.FromNFTTokenAddress) && mintNftRequest.FromNFTTokenAddress!=source.Key)
                throw new ArgumentException("Requested NFT source token account does not match sender and mint.");
            var signature=await new SolanaLegacyTokenClient(rpcClient).SendLegacySplAsync(signer,recipient,mint,1,mintNftRequest.MemoText,requireNft:true);
            response.Result=new SendTransactionResult {TransactionHash=signature};response.IsSaved=true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref response,ex.Message,ex);
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

}
