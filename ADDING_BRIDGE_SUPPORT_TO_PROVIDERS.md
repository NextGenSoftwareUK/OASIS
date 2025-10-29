# 🌉 Adding Bridge Support to OASIS Providers

**Status:** Currently only **SolanaOASIS** has bridge support implemented  
**Date:** October 29, 2025

---

## 📋 Overview

**Q: Will the bridge work for other providers?**  
**A: Not yet - each provider needs to implement the `IOASISBridge` interface.**

The bridge infrastructure is **ready and working**, but each blockchain provider needs to add bridge support. This is a **straightforward process** following the pattern already established with SolanaOASIS.

---

## ✅ What's Already Done

### Bridge Infrastructure (100% Complete)
- ✅ `IOASISBridge` - Generic interface for all blockchains
- ✅ `CrossChainBridgeManager` - Atomic swap orchestration
- ✅ `IExchangeRateService` - Real-time exchange rates
- ✅ Bridge DTOs and Enums
- ✅ Error handling and rollback logic

### SolanaOASIS Bridge (100% Complete)
- ✅ `SolanaBridgeService` - Full implementation
- ✅ Account creation & restoration
- ✅ Balance checking
- ✅ Deposits & withdrawals
- ✅ Transaction status queries

---

## 🏗️ What Each Provider Needs

### Current Status of Providers

| Provider | Bridge Support | Effort Needed |
|----------|----------------|---------------|
| **SolanaOASIS** | ✅ Complete | None |
| **RadixOASIS** | ⚠️ 40% (compilation issues) | Fix SDK issues |
| **EthereumOASIS** | ❌ Not implemented | ~6-8 hours |
| **PolygonOASIS** | ❌ Not implemented | ~6-8 hours |
| **ArbitrumOASIS** | ❌ Not implemented | ~6-8 hours |
| **AvalancheOASIS** | ❌ Not implemented | ~6-8 hours |
| **BaseOASIS** | ❌ Not implemented | ~6-8 hours |
| **OptimismOASIS** | ❌ Not implemented | ~6-8 hours |
| **BNBChainOASIS** | ❌ Not implemented | ~6-8 hours |
| **FantomOASIS** | ❌ Not implemented | ~6-8 hours |
| **CardanoOASIS** | ❌ Not implemented | ~8-10 hours |
| **BitcoinOASIS** | ❌ Not implemented | ~10-12 hours |
| *All others* | ❌ Not implemented | ~6-10 hours each |

---

## 🎯 Implementation Steps

### Step 1: Create Bridge Service Interface (5 minutes)

Create a provider-specific bridge service interface:

```csharp
// Example: IEthereumBridgeService.cs
namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Infrastructure.Services;

public interface IEthereumBridgeService : IOASISBridge
{
    // Add any Ethereum-specific methods here if needed
}
```

### Step 2: Implement Bridge Service (~4-6 hours)

Create the bridge service implementing all `IOASISBridge` methods:

```csharp
// Example: EthereumBridgeService.cs
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Infrastructure.Services;

public class EthereumBridgeService : IEthereumBridgeService
{
    private readonly Web3 _web3;
    private readonly Account _technicalAccount;

    public EthereumBridgeService(Web3 web3, Account technicalAccount)
    {
        _web3 = web3 ?? throw new ArgumentNullException(nameof(web3));
        _technicalAccount = technicalAccount ?? throw new ArgumentNullException(nameof(technicalAccount));
    }

    // 1. Implement GetAccountBalanceAsync
    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(
        string accountAddress, 
        CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        try
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(accountAddress);
            result.Result = Web3.Convert.FromWei(balance.Value);
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error getting balance: {ex.Message}";
            return result;
        }
    }

    // 2. Implement CreateAccountAsync
    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> 
        CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string, string, string)>();
        try
        {
            // Create new Ethereum account
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var address = ecKey.GetPublicAddress();
            
            // Generate mnemonic
            var mnemonic = new Nethereum.HdWallet.Mnemonic(Wordlist.English, WordCount.Twelve);
            
            result.Result = (address, privateKey, mnemonic.ToString());
            result.IsError = false;
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error creating account: {ex.Message}";
            return result;
        }
    }

    // 3. Implement RestoreAccountAsync
    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> 
        RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string, string)>();
        try
        {
            var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, "");
            var account = wallet.GetAccount(0);
            
            result.Result = (account.Address, account.PrivateKey);
            result.IsError = false;
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error restoring account: {ex.Message}";
            return result;
        }
    }

    // 4. Implement WithdrawAsync
    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(
        decimal amount, 
        string senderAccountAddress, 
        string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            var account = new Account(senderPrivateKey);
            var web3 = new Web3(account, _web3.Client);
            
            // Send ETH from sender to technical account
            var receipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(_technicalAccount.Address, amount);
            
            result.Result = new BridgeTransactionResponse
            {
                TransactionHash = receipt.TransactionHash,
                Status = receipt.Status.Value == 1 
                    ? BridgeTransactionStatus.Confirmed 
                    : BridgeTransactionStatus.Failed,
                Amount = amount,
                FromAddress = senderAccountAddress,
                ToAddress = _technicalAccount.Address,
                Timestamp = DateTime.UtcNow
            };
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error withdrawing: {ex.Message}";
            return result;
        }
    }

    // 5. Implement DepositAsync
    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(
        decimal amount, 
        string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            // Send ETH from technical account to receiver
            var receipt = await _web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount);
            
            result.Result = new BridgeTransactionResponse
            {
                TransactionHash = receipt.TransactionHash,
                Status = receipt.Status.Value == 1 
                    ? BridgeTransactionStatus.Confirmed 
                    : BridgeTransactionStatus.Failed,
                Amount = amount,
                FromAddress = _technicalAccount.Address,
                ToAddress = receiverAccountAddress,
                Timestamp = DateTime.UtcNow
            };
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error depositing: {ex.Message}";
            return result;
        }
    }

    // 6. Implement GetTransactionStatusAsync
    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(
        string transactionHash, 
        CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        try
        {
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            
            if (receipt == null)
            {
                result.Result = BridgeTransactionStatus.Pending;
            }
            else
            {
                result.Result = receipt.Status.Value == 1 
                    ? BridgeTransactionStatus.Confirmed 
                    : BridgeTransactionStatus.Failed;
            }
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error getting transaction status: {ex.Message}";
            return result;
        }
    }
}
```

### Step 3: Integrate with Main Provider (~1-2 hours)

Add bridge service to your main provider class:

```csharp
public class EthereumOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider
{
    private EthereumBridgeService _bridgeService;
    
    // Add property to expose bridge service
    public IEthereumBridgeService BridgeService 
    { 
        get 
        { 
            if (_bridgeService == null && Web3Client != null)
                _bridgeService = new EthereumBridgeService(Web3Client, _technicalAccount);
            return _bridgeService;
        }
    }
    
    // Rest of provider implementation...
}
```

### Step 4: Update Exchange Rate Service (10 minutes)

Add your token to the exchange rate service:

```csharp
// In CoinGeckoExchangeRateService constructor or configuration
_coinIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "SOL", "solana" },
    { "XRD", "radix" },
    { "ETH", "ethereum" },  // ← Add your token
    { "MATIC", "matic-network" },  // For Polygon
    { "AVAX", "avalanche-2" },     // For Avalanche
    // etc.
};
```

### Step 5: Test (~1 hour)

Create basic tests:

```csharp
var bridgeManager = new CrossChainBridgeManager(
    solanaBridge: solanaProvider.BridgeService,
    radixBridge: ethereumProvider.BridgeService  // ← Your new bridge!
);

var request = new CreateBridgeOrderRequest
{
    FromToken = "SOL",
    ToToken = "ETH",  // ← Your token
    Amount = 1.0m,
    DestinationAddress = "0x...",  // Ethereum address
    UserId = userId
};

var result = await bridgeManager.CreateBridgeOrderAsync(request);
```

---

## 📝 IOASISBridge Interface Reference

```csharp
public interface IOASISBridge
{
    // 1. Get account balance in native token
    Task<OASISResult<decimal>> GetAccountBalanceAsync(
        string accountAddress, 
        CancellationToken token = default);

    // 2. Create new account (returns keys and seed phrase)
    Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> 
        CreateAccountAsync(CancellationToken token = default);

    // 3. Restore account from seed phrase
    Task<OASISResult<(string PublicKey, string PrivateKey)>> 
        RestoreAccountAsync(string seedPhrase, CancellationToken token = default);

    // 4. Withdraw from user account to technical account
    Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(
        decimal amount, 
        string senderAccountAddress, 
        string senderPrivateKey);

    // 5. Deposit from technical account to user account
    Task<OASISResult<BridgeTransactionResponse>> DepositAsync(
        decimal amount, 
        string receiverAccountAddress);

    // 6. Check transaction status
    Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(
        string transactionHash, 
        CancellationToken token = default);
}
```

---

## 🎯 Priority Order for Implementation

### High Priority (EVM Chains - ~6-8 hours each)
These share similar code patterns:
1. **EthereumOASIS** - Base implementation
2. **PolygonOASIS** - Copy from Ethereum
3. **ArbitrumOASIS** - Copy from Ethereum
4. **OptimismOASIS** - Copy from Ethereum
5. **AvalancheOASIS** - Minor adjustments
6. **BaseOASIS** - Copy from Ethereum
7. **BNBChainOASIS** - Copy from Ethereum
8. **FantomOASIS** - Copy from Ethereum

### Medium Priority (~8-10 hours each)
9. **CardanoOASIS** - Different architecture
10. **NEAROASIS** - Different APIs
11. **SuiOASIS** - Newer blockchain

### Lower Priority (~10-12 hours each)
12. **BitcoinOASIS** - UTXO model (more complex)
13. **PolkadotOASIS** - Substrate framework
14. **CosmosBlockChainOASIS** - IBC protocol

---

## ⚡ Quick Start Template

Want to add bridge support quickly? Use this template:

```bash
# 1. Copy SolanaBridgeService as template
cp Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/Infrastructure/Services/Solana/SolanaBridgeService.cs \
   Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.YOUR_PROVIDER/Infrastructure/Services/YOUR_CHAIN/YOUR_CHAINBridgeService.cs

# 2. Replace Solana-specific code with your blockchain SDK
# 3. Update namespaces
# 4. Implement the 6 required methods
# 5. Add to main provider class
# 6. Test!
```

---

## 🔧 Key Considerations

### 1. Technical Account Management
- Each bridge service needs a "technical account" that temporarily holds funds during swaps
- This account must be securely managed
- Consider using multi-sig or threshold signatures for production

### 2. Gas/Fee Handling
- Different blockchains have different fee structures
- ETH uses gas, Solana uses lamports, etc.
- Make sure to account for fees in your calculations

### 3. Transaction Confirmation
- Different blockchains have different confirmation times
- Ethereum: ~15 seconds
- Solana: <1 second
- Bitcoin: ~10 minutes
- Adjust timeouts accordingly

### 4. Native Token Units
- ETH uses Wei (10^18)
- Solana uses Lamports (10^9)
- BTC uses Satoshis (10^8)
- Always convert to decimal for consistency

---

## ✅ Checklist for Each Provider

Before marking a provider as "bridge-ready":

- [ ] Bridge service class created
- [ ] All 6 IOASISBridge methods implemented
- [ ] Integrated with main provider class
- [ ] Token added to exchange rate service
- [ ] Basic tests passing
- [ ] Balance checks work
- [ ] Account creation works
- [ ] Deposits/withdrawals work
- [ ] Transaction status queries work
- [ ] Documentation updated

---

## 📚 Reference Implementation

**Best reference:** `SolanaBridgeService.cs`
- Location: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/Infrastructure/Services/Solana/`
- ~330 lines of well-commented code
- All methods fully implemented
- Error handling examples
- Unit conversion examples

---

## 🚀 Estimated Timeline

### Single Provider (e.g., EthereumOASIS)
- Setup: 30 minutes
- Implementation: 4-6 hours
- Testing: 1 hour
- Documentation: 30 minutes
- **Total: ~6-8 hours**

### All EVM Chains (8 providers)
- First provider: 6-8 hours
- Each additional EVM provider: 2-3 hours (copy & adjust)
- **Total: ~22-30 hours**

### All Major Chains (15 providers)
- EVM chains: ~30 hours
- Non-EVM chains: ~60 hours
- **Total: ~90 hours (~2-3 weeks for one developer)**

---

## 💡 Tips for Success

1. **Start with one EVM chain** (Ethereum) - then others are easy
2. **Use SolanaBridgeService as template** - it's complete and well-tested
3. **Test on testnet first** - never test with real funds
4. **Implement safety checks** - validate addresses, check balances, etc.
5. **Handle errors gracefully** - atomic swaps depend on proper rollback
6. **Document your code** - future developers will thank you

---

## 🆘 Need Help?

If you get stuck:
1. Check `SolanaBridgeService.cs` for reference
2. Review `IOASISBridge.cs` interface
3. Look at `CrossChainBridgeManager.cs` to see how it's used
4. Check blockchain-specific SDK documentation

---

**Document Created:** October 29, 2025  
**Status:** Ready for implementation  
**Next Step:** Choose a provider and follow the steps above!

