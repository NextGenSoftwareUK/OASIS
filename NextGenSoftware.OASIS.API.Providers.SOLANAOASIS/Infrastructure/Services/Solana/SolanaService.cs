using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.Common;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Metaplex.NFT;
using Solnet.Metaplex.NFT.Library;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public sealed class SolanaService(Account oasisAccount, IRpcClient rpcClient) : ISolanaService
{
    private const uint SellerFeeBasisPoints = 500;
    private const byte CreatorShare = 100;
    private const int MintAmountMax = 1;

    private readonly Account _oasisAccount = oasisAccount;
    private readonly IRpcClient _rpcClient = rpcClient;
    private readonly List<Creator> _creators =
    [
        new(oasisAccount.PublicKey, share: CreatorShare, verified: true)
    ];

    public async Task<OASISResult<ExchangeTokenResult>> ExchangeTokens(ExchangeTokenRequest exchangeTokenRequest)
    {
        OASISResult<ExchangeTokenResult> response = new();
        try
        {
            (bool success, string res) = exchangeTokenRequest.IsRequestValid();
            if (!success)
            {
                response.Message = res;
                response.IsError = true;
                OASISErrorHandling.HandleError(ref response, res);
                return response;
            }

            RequestResult<Solnet.Rpc.Messages.ResponseValue<Solnet.Rpc.Models.LatestBlockHash>> blockHash = await _rpcClient.GetLatestBlockHashAsync();

            PublicKey mintAccount = new(exchangeTokenRequest.MintAccount.PublicKey);
            PublicKey fromAccount = new(exchangeTokenRequest.FromAccount.PublicKey);
            PublicKey toAccount = new(exchangeTokenRequest.ToAccount.PublicKey);

            byte[] tx = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(fromAccount).
                AddInstruction(TokenProgram.InitializeAccount(
                    toAccount,
                    mintAccount,
                    fromAccount)).
                AddInstruction(TokenProgram.Transfer(
                    fromAccount,
                    toAccount,
                    exchangeTokenRequest.Amount,
                    fromAccount)).
                AddInstruction(MemoProgram.NewMemo(fromAccount, exchangeTokenRequest.MemoText)).
                Build(oasisAccount);

            RequestResult<string> sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
            if (!sendTransactionResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransactionResult.Reason;
                OASISErrorHandling.HandleError(ref response, response.Message);
                return response;
            }
            response.Result = new ExchangeTokenResult(sendTransactionResult.Result);
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

    public async Task<OASISResult<MintNftResult>> MintNftAsync(MintNFTTransactionRequestForProvider mintNftRequest)
    {
        OASISResult<MintNftResult> response = new();
        try
        {
            MetadataClient metadataClient = new(_rpcClient);
            Account mintAccount = new();

            // ulong minBalanceForExemptionMint = (await _rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize)).Result;
            // RequestResult<Solnet.Rpc.Messages.ResponseValue<Solnet.Rpc.Models.LatestBlockHash>> blockHash = await _rpcClient.GetLatestBlockHashAsync();

            // TransactionBuilder txBuilder = new TransactionBuilder()
            //     .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
            //     .SetFeePayer(_oasisAccount.PublicKey)
            //     .AddInstruction(SystemProgram.CreateAccount(
            //         fromAccount: _oasisAccount.PublicKey,
            //         newAccountPublicKey: mintAccount.PublicKey,
            //         lamports: minBalanceForExemptionMint,
            //         space: TokenProgram.MintAccountDataSize,
            //         programId: TokenProgram.ProgramIdKey
            //     ))
            //     .AddInstruction(TokenProgram.InitializeMint(
            //         mint: mintAccount.PublicKey,
            //         decimals: 0,
            //         mintAuthority: _oasisAccount.PublicKey,
            //         freezeAuthority: _oasisAccount.PublicKey
            //     ));

            // await _rpcClient.SendTransactionAsync(txBuilder.Build(_oasisAccount));

            Metadata tokenMetadata = new()
            {
                name = mintNftRequest.Title,
                symbol = mintNftRequest.Symbol,
                sellerFeeBasisPoints = SellerFeeBasisPoints,
                uri = mintNftRequest.JSONUrl,
                creators = _creators
            };

            RequestResult<string> createNftResult = await metadataClient.CreateNFT(
                ownerAccount: _oasisAccount,
                mintAccount: mintAccount,
                TokenStandard.NonFungible,
                tokenMetadata,
                isMasterEdition: true,
                isMutable: true);

            if (!createNftResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = createNftResult.Reason;
                OASISErrorHandling.HandleError(ref response, response.Message);
                return response;
            }

            RequestResult<string> mintNftResult = await metadataClient.Mint(
                ownerAccount: _oasisAccount,
                mintAccount,
                isMasterEdition: true,
                mintAmount: MintAmountMax);

            if (!mintNftResult.WasSuccessful &&
                mintNftResult.ServerErrorCode != -32602 &&
                mintNftResult.Reason != "invalid transaction: Transaction failed to sanitize accounts offsets correctly")
            {
                response.IsError = true;
                response.Message = mintNftResult.Reason;
                OASISErrorHandling.HandleError(ref response, response.Message);
                return response;
            }

            response.IsSaved = true;
            response.IsError = false;
            response.Result = new MintNftResult(mintNftResult.Result ?? createNftResult.Result);
        }
        catch (Exception e)
        {
            response.IsError = true;
            response.Message = e.Message;
            OASISErrorHandling.HandleError(ref response, e.Message);
        }
        return response;
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
            RequestResult<Solnet.Rpc.Messages.ResponseValue<Solnet.Rpc.Models.LatestBlockHash>> blockHash = await _rpcClient.GetLatestBlockHashAsync();

            byte[] tx = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(fromAccount).
                AddInstruction(MemoProgram.NewMemo(fromAccount, sendTransactionRequest.MemoText)).
                AddInstruction(SystemProgram.Transfer(fromAccount, toAccount, sendTransactionRequest.Lampposts)).
                Build(oasisAccount);

            RequestResult<string> sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
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

    public async Task<OASISResult<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest)
    {
        OASISResult<GetNftMetadataResult> response = new();
        try
        {
            PublicKey nftAccount = new(getNftMetadataRequest.AccountAddress);
            MetadataAccount metadataAccount = await MetadataAccount.GetAccount(_rpcClient, nftAccount);

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

    public async Task<OASISResult<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest getNftWalletRequest)
    {
        OASISResult<GetNftWalletResult> response = new();
        try
        {
            PublicKey ownerAccount = new(getNftWalletRequest.OwnerAccount.PublicKey);

            TokenMintResolver tokens = new();
            tokens.Add(new TokenDef(getNftWalletRequest.MintToken, getNftWalletRequest.MintName, getNftWalletRequest.MintSymbol, getNftWalletRequest.MintDecimal));

            TokenWallet tokenWallet = await TokenWallet.LoadAsync(_rpcClient, tokens, ownerAccount);
            TokenWalletBalance[] balances = tokenWallet.Balances();
            Solnet.Extensions.Models.TokenWalletFilterList sublist = tokenWallet.TokenAccounts().WithSymbol(getNftWalletRequest.MintSymbol).WithMint(getNftWalletRequest.MintToken);
            if (!string.IsNullOrEmpty(getNftWalletRequest.MintSymbol))
                sublist = sublist.WithSymbol(getNftWalletRequest.MintSymbol);
            if (!string.IsNullOrEmpty(getNftWalletRequest.MintToken))
                sublist = sublist.WithMint(getNftWalletRequest.MintToken);

            response.Result = new GetNftWalletResult()
            {
                Accounts = sublist,
                Balances = balances
            };
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
            RequestResult<Solnet.Rpc.Messages.ResponseValue<Solnet.Rpc.Models.LatestBlockHash>> blockHash =
                await rpcClient.GetLatestBlockHashAsync();

            PublicKey receiverTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(new PublicKey(mintNftRequest.ToWalletAddress), new PublicKey(mintNftRequest.TokenAddress));
            PublicKey senderTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(new PublicKey(mintNftRequest.FromWalletAddress), new PublicKey(mintNftRequest.TokenAddress));

            RequestResult<Solnet.Rpc.Messages.ResponseValue<Solnet.Rpc.Models.AccountInfo>> accountInfo = await rpcClient.GetAccountInfoAsync(receiverTokenAccount);
            if (accountInfo.Result == null)
            {
                Solnet.Rpc.Models.TransactionInstruction createAccountTransaction = AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                    new PublicKey(mintNftRequest.FromWalletAddress), new PublicKey(mintNftRequest.ToWalletAddress), new PublicKey(mintNftRequest.TokenAddress));
                byte[] createAccountTransactionBytes = new TransactionBuilder()
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(new PublicKey(mintNftRequest.FromWalletAddress))
                    .AddInstruction(createAccountTransaction)
                    .Build(_oasisAccount);
                await rpcClient.SendTransactionAsync(createAccountTransactionBytes);
            }

            Solnet.Rpc.Models.TransactionInstruction transferTransaction = TokenProgram.Transfer(
                senderTokenAccount,
                receiverTokenAccount,
                (ulong)mintNftRequest.Amount,
                new PublicKey(mintNftRequest.FromWalletAddress)
            );

            byte[] transactionBytes = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(new PublicKey(mintNftRequest.FromWalletAddress))
                .AddInstruction(transferTransaction)
                .Build(_oasisAccount);

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
}