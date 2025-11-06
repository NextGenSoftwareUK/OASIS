# OASIS Universal Token Bridge - Test Harness

Interactive console application for testing cross-chain token swaps between blockchains.

## 🎯 Purpose

Test and demonstrate the OASIS Universal Token Bridge functionality:
- ✅ Create accounts on different blockchains
- ✅ Check balances
- ✅ Execute atomic cross-chain swaps (SOL ↔ XRD)
- ✅ Automatic rollback on failures

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Terminal/Command Prompt

### Run the Test Harness

```bash
cd /Volumes/Storage/OASIS_CLEAN/NextGenSoftware.OASIS.API.Bridge.TestHarness
dotnet restore
dotnet run
```

## 🔧 Current Status

### ✅ Working
- Solana bridge service (100% functional)
- Account creation (Solana)
- Balance checking (Solana)
- Network connectivity (Devnet)

### ⏳ Pending
- Radix bridge service (compilation issues to fix)
- Full cross-chain swaps (awaiting RadixOASIS completion)
- Exchange rate integration

## 📋 Test Menu Options

### Account Management
- **[1] Create New Solana Account** - Generate new Solana wallet with seed phrase
- **[2] Create New Radix Account** - Generate new Radix wallet (pending)

### Balance Checks
- **[3] Check Solana Balance** - Query SOL balance for any address
- **[4] Check Radix Balance** - Query XRD balance (pending)

### Bridge Operations
- **[5] Test SOL → XRD Swap** - Cross-chain swap from Solana to Radix (pending)
- **[6] Test XRD → SOL Swap** - Cross-chain swap from Radix to Solana (pending)

### Utilities
- **[7] View Configuration** - Display current network and provider status
- **[0] Exit** - Close the application

## 🔐 Security Notes

⚠️ **TESTNET ONLY** - This harness uses test networks:
- Solana: Devnet
- Radix: StokNet

🔒 **Private Keys**: Generated in-memory, not persisted to disk

💡 **Funding**: Get test tokens from faucets:
- Solana Devnet: https://faucet.solana.com
- Radix StokNet: https://stokenet-console.radixdlt.com/

## 🏗️ Architecture

```
Bridge Test Harness
├── Solana Provider (SolanaOASIS)
│   └── SolanaBridgeService (IOASISBridge)
├── Radix Provider (RadixOASIS) [pending]
│   └── RadixBridgeService (IOASISBridge)
└── CrossChainBridgeManager
    ├── Exchange rate service
    ├── Atomic swap orchestration
    └── Auto-rollback on failure
```

## 🧪 Testing Workflow

### 1. Create Test Accounts
```
Select [1] → Create Solana Account
Save the seed phrase and public key
Fund with devnet SOL from faucet
```

### 2. Verify Balances
```
Select [3] → Check Solana Balance
Enter the public key from step 1
Confirm SOL balance appears
```

### 3. Execute Swap (when ready)
```
Select [5] → Test SOL → XRD Swap
Enter amount and destination address
Monitor transaction progress
Verify XRD received or SOL returned on failure
```

## 📁 Project Structure

```
NextGenSoftware.OASIS.API.Bridge.TestHarness/
├── Program.cs                   - Main test harness application
├── README.md                    - This file
└── *.csproj                     - Project configuration
```

## 🔗 Related Files

- **Bridge Manager**: `NextGenSoftware.OASIS.API.Core/Managers/Bridge/CrossChainBridgeManager.cs`
- **Bridge Interface**: `NextGenSoftware.OASIS.API.Core/Managers/Bridge/Interfaces/IOASISBridge.cs`
- **Solana Bridge**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/Infrastructure/Services/Solana/SolanaBridgeService.cs`
- **Radix Provider**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/RadixOASIS.cs`

## 🐛 Known Issues

1. **Radix Provider** - Has compilation issues, needs SDK fixes
2. **Exchange Rates** - Currently using hardcoded test values
3. **Database** - Not integrated (stateless mode)

## 📞 Next Steps

To complete full bridge functionality:

1. ✅ Fix RadixOASIS compilation issues
2. ✅ Add RadixOASIS to solution file
3. ✅ Integrate real exchange rate API
4. ✅ Test end-to-end swaps on testnet
5. ✅ Add database persistence (optional)

## 💡 Tips

- Always test on testnet first
- Save seed phrases securely (in a real app)
- Check balances before attempting swaps
- Monitor transaction hashes on block explorers

## 🆘 Troubleshooting

**Issue**: "Failed to initialize providers"
- **Fix**: Check network connectivity, verify RPC endpoints are accessible

**Issue**: "Account has no balance"
- **Fix**: Fund account from testnet faucet

**Issue**: "Radix provider not initialized"
- **Fix**: This is expected - RadixOASIS needs compilation fixes first

---

**Version**: 1.0  
**Date**: November 3, 2025  
**Network**: TESTNET ONLY



