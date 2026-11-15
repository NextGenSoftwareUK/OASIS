using System;
using System.Globalization;
using System.Threading.Tasks;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request) =>
        LockNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        if (request == null || request.LockedByAvatarId == Guid.Empty)
        {
            OASISErrorHandling.HandleError(ref result, "An avatar with a Bitcoin signing wallet is required to lock an inscription.");
            return result;
        }

        var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.LockedByAvatarId, ProviderType.Value, showPrivateKeys: true);
        if (wallet.IsError || string.IsNullOrWhiteSpace(wallet.Result?.PrivateKey))
        {
            OASISErrorHandling.HandleError(ref result, "The locking avatar has no Bitcoin signing wallet.");
            return result;
        }

        return await LockOrdinalForBridgeAsync(request.NFTTokenAddress, wallet.Result.WalletAddress, wallet.Result.PrivateKey);
    }

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> LockOrdinalForBridgeAsync(string inscriptionId, string sender, string privateKey)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            var pool = ReadOrdinalCustodyPool();
            if (string.IsNullOrWhiteSpace(privateKey) || sender == pool.ToString())
                throw new ArgumentException("Bridge custody requires a signing key and a sender distinct from the custody pool.");
            return await MoveOrdinalInscriptionAsync(inscriptionId, sender, pool.ToString(), privateKey, null, null, burn: false);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin inscription custody failed ({ex.GetType().Name}).");
            return result;
        }
    }

    private BitcoinAddress ReadOrdinalCustodyPool()
    {
        var address = Environment.GetEnvironmentVariable("OASIS_BITCOIN_BRIDGE_POOL_ADDRESS");
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(_privateKey))
            throw new InvalidOperationException("Configure a Bitcoin inscription custody address and its signing key.");
        var pool = BitcoinAddress.Create(address, SigningNetwork);
        var signer = new BitcoinSecret(_privateKey, SigningNetwork);
        if (!pool.Equals(signer.GetAddress(ScriptPubKeyType.TaprootBIP86)) &&
            !pool.Equals(signer.GetAddress(ScriptPubKeyType.Segwit)) &&
            !pool.Equals(signer.GetAddress(ScriptPubKeyType.SegwitP2SH)) &&
            !pool.Equals(signer.GetAddress(ScriptPubKeyType.Legacy)))
            throw new InvalidOperationException("The configured Bitcoin custody address does not belong to the pool signing key.");
        return pool;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request) =>
        UnlockNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (request == null || request.UnlockedByAvatarId == Guid.Empty || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                throw new ArgumentException("An authenticated avatar and inscription ID are required to release Bitcoin custody.");

            var pool = ReadOrdinalCustodyPool();
            var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.UnlockedByAvatarId, ProviderType.Value);
            if (wallet.IsError || string.IsNullOrWhiteSpace(wallet.Result?.WalletAddress))
                throw new InvalidOperationException("The requesting avatar has no Bitcoin wallet.");

            var inscription = await ReadOrdinalInscriptionAsync(request.NFTTokenAddress);
            if (inscription.IsError)
            {
                result.ErrorCode = inscription.ErrorCode;
                OASISErrorHandling.HandleError(ref result, inscription.Message);
                return result;
            }

            var satpoint = inscription.Result.MetaData["Satpoint"]?.Split(':');
            if (!inscription.Result.MetaData.TryGetValue("CurrentOwnerAddress", out var owner) || owner != pool.ToString() ||
                satpoint == null || satpoint.Length != 3 || satpoint[1] != "0" ||
                !uint256.TryParse(satpoint[0], out var lockId) ||
                !long.TryParse(satpoint[2], NumberStyles.None, CultureInfo.InvariantCulture, out var offset))
                throw new InvalidOperationException("The inscription is not in a recognized Bitcoin custody output.");

            var locking = await ReadVerifiedTransactionAsync(lockId);
            if (locking.Inputs.Count != 2 || locking.Outputs.Count != 2 ||
                locking.Outputs[0].ScriptPubKey != pool.ScriptPubKey || offset >= locking.Outputs[0].Value.Satoshi)
                throw new InvalidOperationException("The custody transaction does not have the expected lock layout.");

            var depositor = BitcoinAddress.Create(wallet.Result.WalletAddress, SigningNetwork);
            foreach (var input in locking.Inputs)
            {
                var previous = await ReadVerifiedTransactionAsync(input.PrevOut.Hash);
                if (input.PrevOut.N >= previous.Outputs.Count ||
                    previous.Outputs[(int)input.PrevOut.N].ScriptPubKey != depositor.ScriptPubKey ||
                    (ReferenceEquals(input, locking.Inputs[0]) && previous.Outputs[(int)input.PrevOut.N].Value != locking.Outputs[0].Value))
                    throw new InvalidOperationException("The requesting avatar does not own the original custody inputs.");
            }
            if (locking.Outputs[1].ScriptPubKey != depositor.ScriptPubKey)
                throw new InvalidOperationException("The lock transaction does not return change to the depositor.");

            return await MoveOrdinalInscriptionAsync(inscription.Result.TokenId, pool.ToString(), depositor.ToString(),
                _privateKey, null, null, burn: false, expectedSource: new OutPoint(lockId, 0));
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin inscription release failed ({ex.GetType().Name}).");
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId,
        string senderAccountAddress, string senderPrivateKey)
    {
        var identifier = string.IsNullOrWhiteSpace(nftTokenAddress) ? tokenId : nftTokenAddress;
        var locked = await LockOrdinalForBridgeAsync(identifier, senderAccountAddress, senderPrivateKey);
        var result = new OASISResult<BridgeTransactionResponse> { ErrorCode = locked.ErrorCode, MetaData = locked.MetaData };
        if (locked.IsError)
        {
            OASISErrorHandling.HandleError(ref result, locked.Message, locked.Exception);
            return result;
        }

        result.Result = new BridgeTransactionResponse
        {
            TransactionId = locked.Result.TransactionResult,
            IsSuccessful = true,
            Status = BridgeTransactionStatus.Pending
        };
        return result;
    }

    public Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId,
        string receiverAccountAddress, string sourceTransactionHash = null)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        OASISErrorHandling.HandleError(ref result, "A verified source-chain NFT lock and asset mapping are required before Bitcoin can settle a cross-chain NFT deposit.");
        return Task.FromResult(result);
    }
}
