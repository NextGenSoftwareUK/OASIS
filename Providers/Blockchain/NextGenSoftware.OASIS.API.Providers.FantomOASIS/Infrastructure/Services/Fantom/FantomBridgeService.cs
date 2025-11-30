using System;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using Nethereum.Signer;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS.Infrastructure.Services.Fantom;

public sealed class FantomBridgeService : IFantomBridgeService
{
    private const decimal WeiPerFTM = 1_000_000_000_000_000_000m;
    private readonly Web3 _web3;
    private readonly Account _technicalAccount;
    private readonly string _hostUri;
    private readonly bool _useTestnet;
    private readonly int _chainId;

    public FantomBridgeService(
        Web3 web3, 
        Account technicalAccount, 
        bool useTestnet = true, // Default to testnet for safety
        string hostUri = null)
    {
        _web3 = web3 ?? throw new ArgumentNullException(nameof(web3));
        _technicalAccount = technicalAccount ?? throw new ArgumentNullException(nameof(technicalAccount));
        _useTestnet = useTestnet;
        
        // Set RPC and chain ID based on network
        if (useTestnet)
        {
            _hostUri = hostUri ?? "https://rpc.testnet.fantom.network";
            _chainId = 4002; // Fantom testnet
        }
        else
        {
            _hostUri = hostUri ?? "https://rpcapi.fantom.network";
            _chainId = 250; // Fantom mainnet
        }
    }

    private decimal ConvertWeiToFTM(System.Numerics.BigInteger weiAmount) => (decimal)weiAmount / WeiPerFTM;

    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (string.IsNullOrWhiteSpace(accountAddress))
            {
                result.IsError = true;
                result.Message = "Account address cannot be null or empty";
                return result;
            }
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(accountAddress);
            result.Result = ConvertWeiToFTM(balance.Value);
            result.IsError = false;
            result.Message = $"Balance: {result.Result} FTM";
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error: {ex.Message}";
            return result;
        }
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string, string, string)>();
        try
        {
            var ecKey = EthECKey.GenerateKey();
            result.Result = (ecKey.GetPublicAddress(), ecKey.GetPrivateKey(), $"fantom seed {Guid.NewGuid().ToString("N")} keys");
            result.IsError = false;
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error: {ex.Message}";
            return result;
        }
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string, string)>();
        try
        {
            var account = new Account(seedPhrase.Trim());
            result.Result = (account.Address, account.PrivateKey);
            result.IsError = false;
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error: {ex.Message}";
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            var senderAccount = new Account(senderPrivateKey);
            var senderWeb3 = new Web3(senderAccount, _hostUri);
            var receipt = await senderWeb3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(_technicalAccount.Address, amount);
            
            var isSuccessful = receipt?.Status?.Value == 1;
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = receipt?.TransactionHash ?? "",
                IsSuccessful = isSuccessful,
                Status = isSuccessful ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.NotFound
            };
            result.IsError = !isSuccessful;
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error: {ex.Message}";
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            var receipt = await _web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount);
            
            var isSuccessful = receipt?.Status?.Value == 1;
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = receipt?.TransactionHash ?? "",
                IsSuccessful = isSuccessful,
                Status = isSuccessful ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.NotFound
            };
            result.IsError = !isSuccessful;
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error: {ex.Message}";
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        try
        {
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            result.Result = receipt == null ? BridgeTransactionStatus.Pending : 
                           (receipt.Status?.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.NotFound);
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error: {ex.Message}";
            return result;
        }
    }
}

