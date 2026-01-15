# MNEE Technical Integration Guide

**Contract Address:** `0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF`  
**Network:** Ethereum Mainnet  
**Token Type:** ERC-20 (USD-backed stablecoin)  
**Documentation:** http://docs.mnee.io/

---

## Contract Overview

MNEE is a standard ERC-20 token contract on Ethereum, deployed using an **upgradeable proxy pattern**. The contract address `0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF` is a `TransparentUpgradeableProxy` that forwards all calls to the underlying ERC-20 implementation contract.

**Important:** We interact with the proxy address directly using standard ERC-20 methods. The proxy automatically forwards calls to the implementation, so from our perspective, it behaves exactly like a standard ERC-20 token.

As a USD-backed stablecoin, MNEE maintains a 1:1 peg with USD and is fully collateralized with U.S. Treasury bills and cash equivalents.

### Key Contract Details

- **Contract Address (Proxy):** `0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF`
- **Contract Type:** TransparentUpgradeableProxy (OpenZeppelin)
- **Implementation:** ERC-20 token (delegated via proxy)
- **Token Symbol:** MNEE
- **Token Name:** MNEE USD Stablecoin
- **Decimals:** 18 (standard ERC-20)
- **Total Supply:** ~98,991,351 MNEE (as of Jan 2026)

### Proxy Pattern Notes

- ✅ **Standard ERC-20 interaction** - Use proxy address as if it's a normal ERC-20
- ✅ **No special handling needed** - Proxy forwards all calls transparently
- ✅ **Upgradeable** - Implementation can be upgraded without changing the address
- ⚠️ **Implementation address** - Can change, but we always use the proxy address

---

## ERC-20 Standard Interface

### Required Methods

```solidity
// ERC-20 Standard Interface
interface IERC20 {
    function totalSupply() external view returns (uint256);
    function balanceOf(address account) external view returns (uint256);
    function transfer(address to, uint256 amount) external returns (bool);
    function allowance(address owner, address spender) external view returns (uint256);
    function approve(address spender, uint256 amount) external returns (bool);
    function transferFrom(address from, address to, uint256 amount) external returns (bool);
    
    event Transfer(address indexed from, address indexed to, uint256 value);
    event Approval(address indexed owner, address indexed spender, uint256 value);
}

// Optional Methods
function name() external view returns (string memory);
function symbol() external view returns (string memory);
function decimals() external view returns (uint8);
```

---

## OASIS Integration Implementation

### 1. MNEE Service Class

**File:** `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/Services/MNEEService.cs`

```csharp
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Services
{
    public class MNEEService
    {
        private const string MNEE_CONTRACT_ADDRESS = "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF";
        private const string MNEE_CONTRACT_ABI = @"[{
            ""constant"": true,
            ""inputs"": [{"name"": ""_owner"", ""type"": ""address""}],
            ""name"": ""balanceOf"",
            ""outputs"": [{"name"": ""balance"", ""type"": ""uint256""}],
            ""type"": ""function""
        }, {
            ""constant"": false,
            ""inputs"": [
                {""name"": ""_to"", ""type"": ""address""},
                {""name"": ""_value"", ""type"": ""uint256""}
            ],
            ""name"": ""transfer"",
            ""outputs"": [{"name"": """", ""type"": ""bool""}],
            ""type"": ""function""
        }, {
            ""constant"": false,
            ""inputs"": [
                {""name"": ""_spender"", ""type"": ""address""},
                {""name"": ""_value"", ""type"": ""uint256""}
            ],
            ""name"": ""approve"",
            ""outputs"": [{"name"": """", ""type"": ""bool""}],
            ""type"": ""function""
        }, {
            ""constant"": true,
            ""inputs"": [
                {""name"": ""_owner"", ""type"": ""address""},
                {""name"": ""_spender"", ""type"": ""address""}
            ],
            ""name"": ""allowance"",
            ""outputs"": [{"name"": """", ""type"": ""uint256""}],
            ""type"": ""function""
        }, {
            ""constant"": true,
            ""inputs"": [],
            ""name"": ""decimals"",
            ""outputs"": [{"name"": """", ""type"": ""uint8""}],
            ""type"": ""function""
        }, {
            ""constant"": true,
            ""inputs"": [],
            ""name"": ""symbol"",
            ""outputs"": [{"name"": """", ""type"": ""string""}],
            ""type"": ""function""
        }]";

        private readonly Web3 _web3;
        private readonly Contract _mneeContract;

        public MNEEService(string rpcUrl)
        {
            _web3 = new Web3(rpcUrl);
            // Note: MNEE uses a proxy contract, but we interact with it using standard ERC-20 ABI
            // The proxy automatically forwards calls to the implementation
            _mneeContract = _web3.Eth.GetContract(MNEE_CONTRACT_ABI, MNEE_CONTRACT_ADDRESS);
        }

        /// <summary>
        /// Get MNEE balance for an Ethereum address
        /// </summary>
        public async Task<OASISResult<decimal>> GetBalanceAsync(string address)
        {
            var result = new OASISResult<decimal>();
            try
            {
                var balanceFunction = _mneeContract.GetFunction("balanceOf");
                var balance = await balanceFunction.CallAsync<BigInteger>(address);
                
                // Convert from wei (18 decimals) to MNEE
                var decimals = 18;
                var balanceDecimal = (decimal)balance / (decimal)Math.Pow(10, decimals);
                
                result.Result = balanceDecimal;
                result.IsError = false;
                result.Message = "Balance retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting MNEE balance: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Transfer MNEE from one address to another
        /// </summary>
        public async Task<OASISResult<string>> TransferAsync(
            string fromPrivateKey,
            string toAddress,
            decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                var account = new Account(fromPrivateKey);
                var web3 = new Web3(account, _web3.Client);
                var contract = web3.Eth.GetContract(MNEE_CONTRACT_ABI, MNEE_CONTRACT_ADDRESS);
                
                var transferFunction = contract.GetFunction("transfer");
                
                // Convert amount to wei (18 decimals)
                var decimals = 18;
                var amountWei = new BigInteger(amount * (decimal)Math.Pow(10, decimals));
                
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
                    OASISErrorHandling.HandleError(ref result, "MNEE transfer transaction failed");
                    return result;
                }

                result.Result = receipt.TransactionHash;
                result.IsError = false;
                result.Message = "MNEE transferred successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error transferring MNEE: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Approve spender to use MNEE on behalf of owner
        /// </summary>
        public async Task<OASISResult<string>> ApproveAsync(
            string ownerPrivateKey,
            string spenderAddress,
            decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                var account = new Account(ownerPrivateKey);
                var web3 = new Web3(account, _web3.Client);
                var contract = web3.Eth.GetContract(MNEE_CONTRACT_ABI, MNEE_CONTRACT_ADDRESS);
                
                var approveFunction = contract.GetFunction("approve");
                
                // Convert amount to wei (18 decimals)
                var decimals = 18;
                var amountWei = new BigInteger(amount * (decimal)Math.Pow(10, decimals));
                
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
                    OASISErrorHandling.HandleError(ref result, "MNEE approval transaction failed");
                    return result;
                }

                result.Result = receipt.TransactionHash;
                result.IsError = false;
                result.Message = "MNEE approval successful";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error approving MNEE: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get allowance for spender
        /// </summary>
        public async Task<OASISResult<decimal>> GetAllowanceAsync(
            string ownerAddress,
            string spenderAddress)
        {
            var result = new OASISResult<decimal>();
            try
            {
                var allowanceFunction = _mneeContract.GetFunction("allowance");
                var allowance = await allowanceFunction.CallAsync<BigInteger>(ownerAddress, spenderAddress);
                
                // Convert from wei to MNEE
                var decimals = 18;
                var allowanceDecimal = (decimal)allowance / (decimal)Math.Pow(10, decimals);
                
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
    }
}
```

### 2. Extend Ethereum Provider

**File:** `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/EthereumOASIS.cs`

Add MNEE-specific methods to the Ethereum provider:

```csharp
public class EthereumOASIS : ... 
{
    private MNEEService _mneeService;

    public async Task<OASISResult<decimal>> GetMNEEBalanceAsync(string address)
    {
        if (_mneeService == null)
            _mneeService = new MNEEService(_hostURI);
        
        return await _mneeService.GetBalanceAsync(address);
    }

    public async Task<OASISResult<string>> TransferMNEEAsync(
        string fromPrivateKey,
        string toAddress,
        decimal amount)
    {
        if (_mneeService == null)
            _mneeService = new MNEEService(_hostURI);
        
        return await _mneeService.TransferAsync(fromPrivateKey, toAddress, amount);
    }
}
```

### 3. API Controller

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/MNEEController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MNEEController : ControllerBase
{
    private readonly EthereumOASIS _ethereumProvider;
    private readonly WalletManager _walletManager;
    private readonly KeyManager _keyManager;

    public MNEEController()
    {
        _ethereumProvider = ProviderManager.Instance.GetProvider(ProviderType.EthereumOASIS) as EthereumOASIS;
        _walletManager = WalletManager.Instance;
        _keyManager = KeyManager.Instance;
    }

    /// <summary>
    /// Get MNEE balance for an avatar
    /// </summary>
    [HttpGet("balance/{avatarId}")]
    public async Task<OASISResult<decimal>> GetBalance(Guid avatarId)
    {
        var result = new OASISResult<decimal>();
        
        // Get avatar's Ethereum wallet
        var walletsResult = await _walletManager.LoadProviderWalletsForAvatarByIdAsync(
            avatarId, 
            ProviderType.EthereumOASIS
        );
        
        if (walletsResult.IsError || walletsResult.Result == null || walletsResult.Result.Count == 0)
        {
            OASISErrorHandling.HandleError(ref result, "No Ethereum wallet found for avatar");
            return result;
        }
        
        var wallet = walletsResult.Result[0];
        return await _ethereumProvider.GetMNEEBalanceAsync(wallet.WalletAddress);
    }

    /// <summary>
    /// Transfer MNEE between avatars or to external address
    /// </summary>
    [HttpPost("transfer")]
    public async Task<OASISResult<string>> Transfer([FromBody] MNEETransferRequest request)
    {
        var result = new OASISResult<string>();
        
        // Get sender's private key
        var privateKeyResult = _keyManager.GetProviderPrivateKeysForAvatarById(
            request.FromAvatarId,
            ProviderType.EthereumOASIS
        );
        
        if (privateKeyResult.IsError || privateKeyResult.Result == null || privateKeyResult.Result.Count == 0)
        {
            OASISErrorHandling.HandleError(ref result, "No private key found for sender");
            return result;
        }
        
        // Get recipient address
        string toAddress;
        if (!string.IsNullOrEmpty(request.ToWalletAddress))
        {
            toAddress = request.ToWalletAddress;
        }
        else if (request.ToAvatarId != Guid.Empty)
        {
            var walletsResult = await _walletManager.LoadProviderWalletsForAvatarByIdAsync(
                request.ToAvatarId,
                ProviderType.EthereumOASIS
            );
            
            if (walletsResult.IsError || walletsResult.Result == null || walletsResult.Result.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "No Ethereum wallet found for recipient");
                return result;
            }
            
            toAddress = walletsResult.Result[0].WalletAddress;
        }
        else
        {
            OASISErrorHandling.HandleError(ref result, "Either ToWalletAddress or ToAvatarId must be provided");
            return result;
        }
        
        // Transfer MNEE
        return await _ethereumProvider.TransferMNEEAsync(
            privateKeyResult.Result[0],
            toAddress,
            request.Amount
        );
    }
}

public class MNEETransferRequest
{
    public Guid FromAvatarId { get; set; }
    public Guid? ToAvatarId { get; set; }
    public string ToWalletAddress { get; set; }
    public decimal Amount { get; set; }
    public string Memo { get; set; }
}
```

---

## Testing

### Test MNEE Balance Check
```bash
curl -k -X GET "https://127.0.0.1:5004/api/mnee/balance/{avatarId}" \
  -H "Authorization: Bearer $TOKEN"
```

### Test MNEE Transfer
```bash
curl -k -X POST "https://127.0.0.1:5004/api/mnee/transfer" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "FromAvatarId": "avatar-id-1",
    "ToAvatarId": "avatar-id-2",
    "Amount": 10.5,
    "Memo": "Payment for services"
  }'
```

---

## Integration with Existing OASIS Features

### A2A Protocol Integration

Extend A2A payment system to use MNEE:

```csharp
// In A2AManager-MNEE.cs
public async Task<OASISResult<IA2AMessage>> SendMNEEPaymentAsync(
    Guid fromAgentId,
    Guid toAgentId,
    decimal amount,
    string description = null)
{
    // 1. Create payment request via A2A Protocol
    var paymentRequest = await SendPaymentRequestAsync(
        fromAgentId,
        toAgentId,
        amount,
        "MNEE",
        description
    );
    
    // 2. Execute MNEE transfer
    var transferResult = await _ethereumProvider.TransferMNEEAsync(
        fromPrivateKey,
        toAddress,
        amount
    );
    
    // 3. Confirm payment via A2A Protocol
    if (!transferResult.IsError)
    {
        await SendPaymentConfirmationAsync(
            fromAgentId,
            toAgentId,
            paymentRequest.Result.MessageId,
            transferResult.Result
        );
    }
    
    return paymentRequest;
}
```

---

## Next Steps

1. ✅ Get full contract ABI from Etherscan
2. ✅ Implement MNEEService class
3. ✅ Add API endpoints
4. ✅ Test with testnet MNEE (if available)
5. ✅ Integrate with A2A Protocol
6. ✅ Build programmable finance features

---

**Status:** 🔧 Implementation Phase  
**Priority:** High (Hackathon Submission)
