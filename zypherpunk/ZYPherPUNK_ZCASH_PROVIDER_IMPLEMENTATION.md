# Zypherpunk - Zcash Provider Implementation Guide

**Purpose:** Technical guide for adding Zcash support to OASIS for Zypherpunk hackathon tracks

---

## 🎯 Overview

This guide shows how to add a Zcash provider to OASIS, enabling all Zypherpunk hackathon tracks. The Zcash provider will support:

- Shielded transactions
- Viewing keys (for auditability)
- Partial notes (for enhanced privacy)
- Cross-chain bridge operations

---

## 📁 File Structure

```
NextGenSoftware.OASIS.API.Providers.ZcashOASIS/
├── ZcashOASIS.cs                    # Main provider class
├── ZcashBridgeService.cs            # Bridge-specific operations
├── ZcashWalletService.cs            # Wallet operations
├── Models/
│   ├── ShieldedTransaction.cs
│   ├── ViewingKey.cs
│   ├── PartialNote.cs
│   └── ZcashAddress.cs
├── Helpers/
│   ├── ZcashRPCClient.cs
│   └── PrivacyHelper.cs
└── ZcashOASIS.csproj
```

---

## 🔧 Implementation

### 1. Main Provider Class

**File:** `ZcashOASIS.cs`

```csharp
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    public class ZcashOASIS : IOASISStorageProvider, IOASISNETProvider
    {
        public string ProviderName => "ZcashOASIS";
        public string ProviderDescription => "Zcash blockchain provider with shielded transaction support";
        public ProviderType ProviderType => ProviderType.ZcashOASIS;
        public bool IsProviderActivated { get; private set; }

        private readonly ZcashRPCClient _rpcClient;
        private readonly ZcashWalletService _walletService;
        private readonly ZcashBridgeService _bridgeService;

        public ZcashOASIS()
        {
            _rpcClient = new ZcashRPCClient();
            _walletService = new ZcashWalletService(_rpcClient);
            _bridgeService = new ZcashBridgeService(_rpcClient);
        }

        // IOASISProvider implementation
        public async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Test connection to Zcash node
                var connectionTest = await _rpcClient.TestConnectionAsync();
                if (connectionTest.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to connect to Zcash node: {connectionTest.Message}";
                    return result;
                }

                IsProviderActivated = true;
                result.Result = true;
                result.Message = "Zcash provider activated successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error activating Zcash provider: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<bool>> DeactivateProviderAsync()
        {
            IsProviderActivated = false;
            return new OASISResult<bool> { Result = true };
        }

        // IOASISStorageProvider implementation
        public async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            // Load holon from Zcash (stored as metadata in shielded transaction)
            // Implementation depends on how holons are stored
            throw new NotImplementedException();
        }

        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ProviderType providerType = ProviderType.Default)
        {
            // Save holon to Zcash (store metadata in shielded transaction)
            throw new NotImplementedException();
        }

        // Zcash-specific methods
        public async Task<OASISResult<ShieldedTransaction>> CreateShieldedTransactionAsync(
            string fromAddress,
            string toAddress,
            decimal amount,
            string memo = null
        )
        {
            var result = new OASISResult<ShieldedTransaction>();
            try
            {
                var tx = await _walletService.CreateShieldedTransactionAsync(
                    fromAddress,
                    toAddress,
                    amount,
                    memo
                );

                result.Result = tx;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<ViewingKey>> GenerateViewingKeyAsync(string address)
        {
            return await _walletService.GenerateViewingKeyAsync(address);
        }

        public async Task<OASISResult<PartialNote>> CreatePartialNoteAsync(
            decimal amount,
            int numberOfParts
        )
        {
            return await _walletService.CreatePartialNoteAsync(amount, numberOfParts);
        }

        // Bridge operations
        public async Task<OASISResult<string>> LockZECForBridgeAsync(
            decimal amount,
            string destinationChain,
            string destinationAddress,
            string viewingKey = null
        )
        {
            return await _bridgeService.LockZECForBridgeAsync(
                amount,
                destinationChain,
                destinationAddress,
                viewingKey
            );
        }
    }
}
```

### 2. RPC Client

**File:** `Helpers/ZcashRPCClient.cs`

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Helpers
{
    public class ZcashRPCClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcUrl;
        private readonly string _rpcUser;
        private readonly string _rpcPassword;

        public ZcashRPCClient()
        {
            _httpClient = new HttpClient();
            // Load from OASIS_DNA.json or environment variables
            _rpcUrl = Environment.GetEnvironmentVariable("ZCASH_RPC_URL") ?? "http://localhost:8232";
            _rpcUser = Environment.GetEnvironmentVariable("ZCASH_RPC_USER") ?? "user";
            _rpcPassword = Environment.GetEnvironmentVariable("ZCASH_RPC_PASSWORD") ?? "password";
        }

        public async Task<OASISResult<bool>> TestConnectionAsync()
        {
            try
            {
                var request = new
                {
                    jsonrpc = "2.0",
                    method = "getinfo",
                    id = 1
                };

                var response = await SendRPCRequestAsync(request);
                return new OASISResult<bool> { Result = true };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        public async Task<OASISResult<string>> SendShieldedTransactionAsync(
            string fromAddress,
            string toAddress,
            decimal amount,
            string memo = null
        )
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "z_sendmany",
                @params = new object[]
                {
                    fromAddress,
                    new[]
                    {
                        new
                        {
                            address = toAddress,
                            amount = amount.ToString("F8"),
                            memo = memo ?? ""
                        }
                    }
                },
                id = 1
            };

            var response = await SendRPCRequestAsync(request);
            // Parse response and return operation ID
            return new OASISResult<string> { Result = response.operationid };
        }

        public async Task<OASISResult<object>> GetTransactionAsync(string txId)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "gettransaction",
                @params = new[] { txId },
                id = 1
            };

            return await SendRPCRequestAsync(request);
        }

        private async Task<object> SendRPCRequestAsync(object request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var authValue = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_rpcUser}:{_rpcPassword}")
            );
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

            var response = await _httpClient.PostAsync(_rpcUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<object>(responseContent);
        }
    }
}
```

### 3. Wallet Service

**File:** `ZcashWalletService.cs`

```csharp
namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    public class ZcashWalletService
    {
        private readonly ZcashRPCClient _rpcClient;

        public ZcashWalletService(ZcashRPCClient rpcClient)
        {
            _rpcClient = rpcClient;
        }

        public async Task<OASISResult<ShieldedTransaction>> CreateShieldedTransactionAsync(
            string fromAddress,
            string toAddress,
            decimal amount,
            string memo = null
        )
        {
            var result = new OASISResult<ShieldedTransaction>();
            try
            {
                var txId = await _rpcClient.SendShieldedTransactionAsync(
                    fromAddress,
                    toAddress,
                    amount,
                    memo
                );

                if (txId.IsError)
                {
                    result.IsError = true;
                    result.Message = txId.Message;
                    return result;
                }

                // Wait for confirmation
                var confirmed = await WaitForConfirmationAsync(txId.Result);

                result.Result = new ShieldedTransaction
                {
                    TransactionId = txId.Result,
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = memo,
                    Confirmed = confirmed
                };
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<ViewingKey>> GenerateViewingKeyAsync(string address)
        {
            var result = new OASISResult<ViewingKey>();
            try
            {
                // Use Zcash RPC to generate viewing key
                // z_exportviewingkey command
                var viewingKey = await _rpcClient.ExportViewingKeyAsync(address);

                result.Result = new ViewingKey
                {
                    Address = address,
                    Key = viewingKey,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<PartialNote>> CreatePartialNoteAsync(
            decimal amount,
            int numberOfParts
        )
        {
            var result = new OASISResult<PartialNote>();
            try
            {
                // Split amount into partial notes for enhanced privacy
                var partAmount = amount / numberOfParts;
                var parts = new List<PartialNotePart>();

                for (int i = 0; i < numberOfParts; i++)
                {
                    parts.Add(new PartialNotePart
                    {
                        Amount = partAmount,
                        Index = i,
                        NoteId = Guid.NewGuid().ToString()
                    });
                }

                result.Result = new PartialNote
                {
                    TotalAmount = amount,
                    NumberOfParts = numberOfParts,
                    Parts = parts,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        private async Task<bool> WaitForConfirmationAsync(string txId, int maxWait = 60)
        {
            for (int i = 0; i < maxWait; i++)
            {
                await Task.Delay(1000);
                var tx = await _rpcClient.GetTransactionAsync(txId);
                if (tx != null && tx.confirmations > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
```

### 4. Bridge Service

**File:** `ZcashBridgeService.cs`

```csharp
namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    public class ZcashBridgeService
    {
        private readonly ZcashRPCClient _rpcClient;

        public ZcashBridgeService(ZcashRPCClient rpcClient)
        {
            _rpcClient = rpcClient;
        }

        public async Task<OASISResult<string>> LockZECForBridgeAsync(
            decimal amount,
            string destinationChain,
            string destinationAddress,
            string viewingKey = null
        )
        {
            var result = new OASISResult<string>();
            try
            {
                // Create bridge lock address (could be a smart contract or special address)
                var bridgeAddress = GetBridgeAddressForChain(destinationChain);

                // Create shielded transaction to bridge address
                var tx = await _rpcClient.SendShieldedTransactionAsync(
                    null, // From address (will use default)
                    bridgeAddress,
                    amount,
                    $"{destinationChain}:{destinationAddress}" // Memo contains destination info
                );

                if (tx.IsError)
                {
                    result.IsError = true;
                    result.Message = tx.Message;
                    return result;
                }

                // Store viewing key if provided (for auditability)
                if (!string.IsNullOrEmpty(viewingKey))
                {
                    await StoreViewingKeyForTransactionAsync(tx.Result, viewingKey);
                }

                result.Result = tx.Result;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        private string GetBridgeAddressForChain(string chain)
        {
            // Return bridge address for specific chain
            // Could be loaded from OASIS_DNA.json
            return chain switch
            {
                "Aztec" => Environment.GetEnvironmentVariable("ZCASH_AZTEC_BRIDGE_ADDRESS"),
                "Miden" => Environment.GetEnvironmentVariable("ZCASH_MIDEN_BRIDGE_ADDRESS"),
                "Solana" => Environment.GetEnvironmentVariable("ZCASH_SOLANA_BRIDGE_ADDRESS"),
                _ => throw new ArgumentException($"Unsupported destination chain: {chain}")
            };
        }

        private async Task StoreViewingKeyForTransactionAsync(string txId, string viewingKey)
        {
            // Store viewing key in holon for auditability
            // This allows auditors to verify transactions without revealing amounts
            var holon = new Holon
            {
                Name = $"Bridge Viewing Key: {txId}",
                HolonType = HolonType.Bridge,
                ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>
                {
                    {
                        ProviderType.ZcashOASIS,
                        new Dictionary<string, string>
                        {
                            { "TransactionId", txId },
                            { "ViewingKey", viewingKey },
                            { "Purpose", "Auditability" }
                        }
                    }
                }
            };

            // Save to MongoDB for fast access, replicate to IPFS for permanence
            await HolonManager.Instance.SaveHolonAsync(holon);
        }
    }
}
```

### 5. Models

**File:** `Models/ShieldedTransaction.cs`

```csharp
namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models
{
    public class ShieldedTransaction
    {
        public string TransactionId { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string Memo { get; set; }
        public bool Confirmed { get; set; }
        public int Confirmations { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ViewingKey
    {
        public string Address { get; set; }
        public string Key { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PartialNote
    {
        public decimal TotalAmount { get; set; }
        public int NumberOfParts { get; set; }
        public List<PartialNotePart> Parts { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PartialNotePart
    {
        public decimal Amount { get; set; }
        public int Index { get; set; }
        public string NoteId { get; set; }
    }
}
```

### 6. OASIS_DNA Configuration

**Add to `OASIS_DNA.json`:**

```json
{
  "StorageProviders": {
    "ZcashOASIS": {
      "ProviderType": "ZcashOASIS",
      "IsEnabled": true,
      "IsDefault": false,
      "ConnectionString": "http://localhost:8232",
      "RPCUser": "user",
      "RPCPassword": "password",
      "Network": "testnet",
      "BridgeAddresses": {
        "Aztec": "zt1...",
        "Miden": "zt1...",
        "Solana": "zt1..."
      }
    }
  },
  "AutoFailOverProviders": "MongoDBOASIS, ZcashOASIS, ArbitrumOASIS",
  "AutoReplicationProviders": "MongoDBOASIS, ZcashOASIS, IPFSOASIS"
}
```

---

## 🧪 Testing

### Unit Tests

```csharp
[Test]
public async Task TestCreateShieldedTransaction()
{
    var provider = new ZcashOASIS();
    await provider.ActivateProviderAsync();

    var result = await provider.CreateShieldedTransactionAsync(
        "zt1...",
        "zt1...",
        1.0m,
        "Test memo"
    );

    Assert.IsFalse(result.IsError);
    Assert.IsNotNull(result.Result);
    Assert.IsTrue(result.Result.Confirmed);
}

[Test]
public async Task TestGenerateViewingKey()
{
    var provider = new ZcashOASIS();
    await provider.ActivateProviderAsync();

    var result = await provider.GenerateViewingKeyAsync("zt1...");

    Assert.IsFalse(result.IsError);
    Assert.IsNotNull(result.Result);
    Assert.IsNotNull(result.Result.Key);
}
```

---

## 🚀 Next Steps

1. **Set Up Zcash Testnet Node**
   - Install Zcash node
   - Configure RPC access
   - Get testnet ZEC

2. **Implement Provider**
   - Follow code structure above
   - Test basic operations
   - Integrate with Provider Manager

3. **Extend Bridge**
   - Add Zcash support to Universal Asset Bridge
   - Implement private bridge operations
   - Test cross-chain swaps

4. **Build Hackathon Solutions**
   - Use provider for all Zypherpunk tracks
   - Leverage OASIS infrastructure
   - Submit solutions

---

## 📚 Resources

- **Zcash RPC API:** https://zcash-rpc.github.io/
- **Zcash Developer Docs:** https://zcash.readthedocs.io/
- **OASIS Provider Guide:** `/Docs/Devs/OASIS_INTEROPERABILITY_ARCHITECTURE.md`

---

**Last Updated:** 2025  
**Status:** Implementation Guide

