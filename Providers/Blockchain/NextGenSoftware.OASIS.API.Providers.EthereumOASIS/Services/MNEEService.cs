using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Services
{
    /// <summary>
    /// Generic ERC-20 token service for interacting with any ERC-20 compatible token contract
    /// Can be used with MNEE or any other ERC-20 token by providing the contract address
    /// Note: MNEE uses a TransparentUpgradeableProxy, but we interact with it using standard ERC-20 ABI
    /// </summary>
    public class MNEEService
    {
        // Default MNEE contract address for convenience
        public const string MNEE_CONTRACT_ADDRESS = "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF";
        private const string ERC20_CONTRACT_ABI = @"[{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":""balance"",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":"""",""type"":""bool""}],""type"":""function""},{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""},{""name"":""_spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""decimals"",""outputs"":[{""name"":"""",""type"":""uint8""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""symbol"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""name"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""},{""constant"":true,""inputs"":[],""name"":""totalSupply"",""outputs"":[{""name"":"""",""type"":""uint256""}],""type"":""function""}]";

        private readonly Web3 _web3;
        private readonly string _defaultContractAddress;

        /// <summary>
        /// Create a generic ERC-20 token service
        /// </summary>
        /// <param name="rpcUrl">Ethereum RPC URL</param>
        /// <param name="contractAddress">Token contract address (defaults to MNEE if not provided)</param>
        public MNEEService(string rpcUrl, string contractAddress = null)
        {
            if (string.IsNullOrWhiteSpace(rpcUrl))
            {
                throw new ArgumentException("RPC URL is required", nameof(rpcUrl));
            }

            _web3 = new Web3(rpcUrl);
            _defaultContractAddress = contractAddress ?? MNEE_CONTRACT_ADDRESS;
        }

        /// <summary>
        /// Get contract instance for a specific token address
        /// </summary>
        private Contract GetContract(string contractAddress = null)
        {
            var address = contractAddress ?? _defaultContractAddress;
            return _web3.Eth.GetContract(ERC20_CONTRACT_ABI, address);
        }

        /// <summary>
        /// Get token balance for an Ethereum address
        /// Generic method that works with any ERC-20 token
        /// </summary>
        /// <param name="address">Ethereum address</param>
        /// <param name="contractAddress">Token contract address (optional, uses default if not provided)</param>
        public async Task<OASISResult<decimal>> GetBalanceAsync(string address, string contractAddress = null)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (string.IsNullOrWhiteSpace(address))
                {
                    OASISErrorHandling.HandleError(ref result, "Address is required");
                    return result;
                }

                var contract = GetContract(contractAddress);
                var balanceFunction = contract.GetFunction("balanceOf");
                var balance = await balanceFunction.CallAsync<BigInteger>(address);
                
                // Get decimals
                var decimalsFunction = contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                
                // Convert from wei to token units
                var multiplier = BigInteger.Pow(10, decimals);
                var balanceDecimal = (decimal)balance / (decimal)multiplier;
                
                result.Result = balanceDecimal;
                result.IsError = false;
                result.Message = "Token balance retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting token balance: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Transfer tokens from one address to another
        /// Generic method that works with any ERC-20 token
        /// </summary>
        /// <param name="fromPrivateKey">Sender's private key</param>
        /// <param name="toAddress">Recipient address</param>
        /// <param name="amount">Amount to transfer</param>
        /// <param name="contractAddress">Token contract address (optional, uses default if not provided)</param>
        public async Task<OASISResult<string>> TransferAsync(
            string fromPrivateKey,
            string toAddress,
            decimal amount,
            string contractAddress = null)
        {
            var result = new OASISResult<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(fromPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Private key is required");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Recipient address is required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                var account = new Account(fromPrivateKey);
                var web3 = new Web3(account, _web3.Client);
                var contractAddressToUse = contractAddress ?? _defaultContractAddress;
                var contract = web3.Eth.GetContract(ERC20_CONTRACT_ABI, contractAddressToUse);
                
                var transferFunction = contract.GetFunction("transfer");
                
                // Get decimals and convert amount to wei
                var decimalsFunction = contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = BigInteger.Pow(10, decimals);
                var amountWei = new BigInteger(amount * (decimal)multiplier);
                
                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    account.Address,
                    new HexBigInteger(60000), // Gas limit
                    null,
                    null,
                    toAddress,
                    amountWei
                );

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, "Token transfer transaction failed");
                    return result;
                }

                result.Result = receipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token transferred successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error transferring token: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Approve spender to use tokens on behalf of owner
        /// Generic method that works with any ERC-20 token
        /// </summary>
        /// <param name="ownerPrivateKey">Owner's private key</param>
        /// <param name="spenderAddress">Spender address</param>
        /// <param name="amount">Amount to approve</param>
        /// <param name="contractAddress">Token contract address (optional, uses default if not provided)</param>
        public async Task<OASISResult<string>> ApproveAsync(
            string ownerPrivateKey,
            string spenderAddress,
            decimal amount,
            string contractAddress = null)
        {
            var result = new OASISResult<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(ownerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Private key is required");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(spenderAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Spender address is required");
                    return result;
                }

                if (amount < 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount cannot be negative");
                    return result;
                }

                var account = new Account(ownerPrivateKey);
                var web3 = new Web3(account, _web3.Client);
                var contractAddressToUse = contractAddress ?? _defaultContractAddress;
                var contract = web3.Eth.GetContract(ERC20_CONTRACT_ABI, contractAddressToUse);
                
                var approveFunction = contract.GetFunction("approve");
                
                // Get decimals and convert amount to wei
                var decimalsFunction = contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = BigInteger.Pow(10, decimals);
                var amountWei = new BigInteger(amount * (decimal)multiplier);
                
                var receipt = await approveFunction.SendTransactionAndWaitForReceiptAsync(
                    account.Address,
                    new HexBigInteger(60000),
                    null,
                    null,
                    spenderAddress,
                    amountWei
                );

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, "Token approval transaction failed");
                    return result;
                }

                result.Result = receipt.TransactionHash;
                result.IsError = false;
                result.Message = "Token approval successful";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error approving token: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get allowance for spender
        /// Generic method that works with any ERC-20 token
        /// </summary>
        /// <param name="ownerAddress">Owner address</param>
        /// <param name="spenderAddress">Spender address</param>
        /// <param name="contractAddress">Token contract address (optional, uses default if not provided)</param>
        public async Task<OASISResult<decimal>> GetAllowanceAsync(
            string ownerAddress,
            string spenderAddress,
            string contractAddress = null)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (string.IsNullOrWhiteSpace(ownerAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Owner address is required");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(spenderAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Spender address is required");
                    return result;
                }

                var contract = GetContract(contractAddress);
                var allowanceFunction = contract.GetFunction("allowance");
                var allowance = await allowanceFunction.CallAsync<BigInteger>(ownerAddress, spenderAddress);
                
                // Get decimals and convert from wei to token units
                var decimalsFunction = contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = BigInteger.Pow(10, decimals);
                var allowanceDecimal = (decimal)allowance / (decimal)multiplier;
                
                result.Result = allowanceDecimal;
                result.IsError = false;
                result.Message = "Allowance retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting allowance: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get token information (name, symbol, decimals, total supply)
        /// Generic method that works with any ERC-20 token
        /// </summary>
        /// <param name="contractAddress">Token contract address (optional, uses default if not provided)</param>
        public async Task<OASISResult<MNEETokenInfo>> GetTokenInfoAsync(string contractAddress = null)
        {
            var result = new OASISResult<MNEETokenInfo>();
            try
            {
                var contractAddressToUse = contractAddress ?? _defaultContractAddress;
                var contract = GetContract(contractAddressToUse);
                
                var nameFunction = contract.GetFunction("name");
                var symbolFunction = contract.GetFunction("symbol");
                var decimalsFunction = contract.GetFunction("decimals");
                var totalSupplyFunction = contract.GetFunction("totalSupply");

                var name = await nameFunction.CallAsync<string>();
                var symbol = await symbolFunction.CallAsync<string>();
                var decimals = await decimalsFunction.CallAsync<byte>();
                var totalSupply = await totalSupplyFunction.CallAsync<BigInteger>();

                var multiplier = BigInteger.Pow(10, decimals);
                var totalSupplyDecimal = (decimal)totalSupply / (decimal)multiplier;

                result.Result = new MNEETokenInfo
                {
                    Name = name,
                    Symbol = symbol,
                    Decimals = decimals,
                    TotalSupply = totalSupplyDecimal,
                    ContractAddress = contractAddressToUse
                };
                result.IsError = false;
                result.Message = "Token info retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting token info: {ex.Message}", ex);
            }
            return result;
        }
    }

    /// <summary>
    /// Generic ERC-20 token information
    /// </summary>
    public class MNEETokenInfo
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public byte Decimals { get; set; }
        public decimal TotalSupply { get; set; }
        public string ContractAddress { get; set; }
    }
}
