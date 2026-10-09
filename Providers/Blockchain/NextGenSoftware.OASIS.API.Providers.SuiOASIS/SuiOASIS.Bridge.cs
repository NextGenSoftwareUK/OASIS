using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public partial class SuiOASIS
{
    // These interface methods implement custodial settlement. Cross-chain source
    // verification and authorization belong to the bridge coordinator, not a mint.
    private async Task<string> BridgeWalletAsync()
    {
        if (string.IsNullOrWhiteSpace(_privateKey))
            throw new InvalidOperationException("A configured Sui bridge custody signing key is required.");
        return (await InvokeSdkAsync("restoreKey")).GetProperty("address").GetString();
    }

    private static OASISResult<BridgeTransactionResponse> CommittedBridge(string digest)
    {
        if (string.IsNullOrWhiteSpace(digest)) throw new InvalidOperationException("No committed Sui bridge digest was returned.");
        return new OASISResult<BridgeTransactionResponse>(new BridgeTransactionResponse
        {
            TransactionId = digest, IsSuccessful = true, Status = BridgeTransactionStatus.Completed
        });
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            var custody = await BridgeWalletAsync();
            if (senderAccountAddress == custody) throw new ArgumentException("Withdrawal sender must differ from the custody wallet.");
            var committed = await SendSuiAsync(senderAccountAddress, custody, amount, senderPrivateKey);
            if (committed.IsError) throw new InvalidOperationException(committed.Message);
            return CommittedBridge(committed.Result.TransactionResult);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            var custody = await BridgeWalletAsync();
            if (receiverAccountAddress == custody) throw new ArgumentException("Deposit receiver must differ from the custody wallet.");
            var committed = await SendSuiAsync(custody, receiverAccountAddress, amount, _privateKey);
            if (committed.IsError) throw new InvalidOperationException(committed.Message);
            return CommittedBridge(committed.Result.TransactionResult);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId,
        string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (nftTokenAddress != _contractAddress) throw new ArgumentException("NFT collection must be the configured Sui package ID.");
            if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                throw new ArgumentException("NFT sender address and signing key are required.");
            var custody = await BridgeWalletAsync();
            if (senderAccountAddress == custody) throw new ArgumentException("NFT withdrawal sender must differ from the custody wallet.");
            var committed = await InvokeSdkAsync("nftSend", new { tokenId, fromWalletAddress = senderAccountAddress,
                privateKey = senderPrivateKey, recipient = custody });
            return CommittedBridge(committed.GetProperty("transactionHash").GetString());
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId,
        string receiverAccountAddress, string sourceTransactionHash = null)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (nftTokenAddress != _contractAddress) throw new ArgumentException("NFT collection must be the configured Sui package ID.");
            var custody = await BridgeWalletAsync();
            if (receiverAccountAddress == custody) throw new ArgumentException("NFT deposit receiver must differ from the custody wallet.");
            // A supplied source digest must prove this exact NFT entered custody on Sui.
            // Foreign-chain proof cannot be verified by a Sui node and must not be accepted here.
            var committed = await InvokeSdkAsync("nftSend", new { tokenId, fromWalletAddress = custody,
                privateKey = _privateKey, recipient = receiverAccountAddress, sourceTransactionHash });
            return CommittedBridge(committed.GetProperty("transactionHash").GetString());
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }
}
