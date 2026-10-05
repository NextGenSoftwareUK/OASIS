# Unified Wallet Feature

## Overview

The Unified Wallet feature allows users to create a single wallet that houses all individual wallets from various blockchains. Instead of managing separate wallets for each chain, users can generate all wallets from a single recovery phrase (mnemonic seed).

## How It Works

### Concept

A **Unified Wallet** is a master wallet that:
- Uses a single 12-word recovery phrase (mnemonic)
- Generates wallets for all supported blockchains automatically
- Provides a unified interface to manage all chain wallets
- Simplifies backup and recovery (one phrase for everything)

### Supported Blockchains

The unified wallet automatically creates wallets for:
- **Solana** (SolanaOASIS)
- **Ethereum** (EthereumOASIS)
- **Polygon** (PolygonOASIS)
- **Arbitrum** (ArbitrumOASIS)
- **Zcash** (ZcashOASIS)

## User Flow

### Creating a Unified Wallet

1. **Navigate to Create Wallet**
   - Click "Create Wallet" from the home screen
   - Select "Create Unified Wallet" (recommended option)

2. **Generate Recovery Phrase**
   - System generates a 12-word mnemonic phrase
   - User must save this phrase securely

3. **Confirm Backup**
   - User confirms they have saved the recovery phrase
   - This is required before wallet creation

4. **Wallet Generation**
   - System generates keypairs for all supported chains
   - Links keys using the Keys API
   - Creates wallets for each blockchain
   - All wallets are linked to the user's avatar

5. **Completion**
   - All wallets appear in the wallet list
   - User can use any chain wallet immediately
   - Total balance shows sum across all chains

## Technical Implementation

### Key Components

#### 1. Unified Wallet Service (`lib/unifiedWallet.ts`)

```typescript
// Generate mnemonic phrase
generateMnemonic(wordCount: 12 | 24): string

// Create unified wallet
createUnifiedWallet(avatarId: string, mnemonic?: string): Promise<UnifiedWallet>

// Import unified wallet from mnemonic
importUnifiedWallet(avatarId: string, mnemonic: string): Promise<UnifiedWallet>
```

#### 2. Create Wallet Screen (`components/wallet/CreateWalletScreen.tsx`)

Enhanced with unified wallet creation flow:
- New "unified" step in the wallet creation process
- Mnemonic generation and display
- Confirmation checkbox for backup
- Progress indication during wallet generation

### API Integration

The unified wallet uses:
- **Keys API**: `generateKeypair()` and `linkKeys()` for each chain
- **Wallet API**: `loadWalletsById()` to retrieve created wallets

### Data Structure

```typescript
interface UnifiedWallet {
  id: string;                    // Unique unified wallet ID
  avatarId: string;             // Linked avatar ID
  mnemonic: string;              // Recovery phrase (should be encrypted in production)
  wallets: Partial<Record<ProviderType, Wallet[]>>;  // All chain wallets
  createdAt: string;             // Creation timestamp
  totalBalance: number;          // Sum of all chain balances
}
```

## Security Considerations

### Current Implementation

1. **Mnemonic Generation**
   - Currently uses a simplified word list
   - **TODO**: Integrate proper BIP39 library for production

2. **Key Derivation**
   - Currently generates independent keypairs per chain
   - **TODO**: Implement BIP44 derivation paths for true HD wallet support

3. **Storage**
   - Mnemonic should be encrypted before storage
   - Never stored in plain text
   - User must backup manually

### Recommended Improvements

1. **BIP39 Integration**
   ```bash
   npm install bip39
   ```
   - Use standard BIP39 word list
   - Proper entropy generation
   - Checksum validation

2. **BIP44 Derivation**
   ```typescript
   // Derive keys using BIP44 paths
   // m/44'/coin_type'/account'/change/address_index
   // Example: m/44'/501'/0'/0/0 (Solana)
   // Example: m/44'/60'/0'/0/0 (Ethereum)
   ```

3. **Encrypted Storage**
   - Encrypt mnemonic with user password
   - Store encrypted version only
   - Never log or display full mnemonic in production

## Benefits

### For Users

✅ **Simplified Management**: One recovery phrase for all chains  
✅ **Easy Backup**: Single phrase to backup instead of multiple keys  
✅ **Unified Balance**: See total balance across all chains  
✅ **Faster Setup**: Create all wallets at once  
✅ **Better UX**: Less complexity, more intuitive  

### For Developers

✅ **Consistent API**: Same Keys API for all chains  
✅ **Scalable**: Easy to add new chains  
✅ **Maintainable**: Centralized wallet creation logic  
✅ **Extensible**: Can add HD wallet features later  

## Future Enhancements

### Phase 1: BIP39/BIP44 Support
- [ ] Integrate BIP39 library for mnemonic generation
- [ ] Implement BIP44 derivation paths
- [ ] True HD wallet support (derive all keys from one seed)

### Phase 2: Advanced Features
- [ ] Import existing mnemonic phrases
- [ ] Export unified wallet (mnemonic + encrypted keys)
- [ ] Multi-account support (multiple accounts per chain)
- [ ] Hardware wallet integration

### Phase 3: UI Improvements
- [ ] Unified wallet dashboard
- [ ] Cross-chain balance view
- [ ] Unified transaction history
- [ ] Chain-specific views with unified navigation

## Usage Example

```typescript
import { createUnifiedWallet } from '@/lib/unifiedWallet';

// Create unified wallet
const unifiedWallet = await createUnifiedWallet(avatarId);

// Access individual chain wallets
const solanaWallet = unifiedWallet.wallets[ProviderType.SolanaOASIS]?.[0];
const ethereumWallet = unifiedWallet.wallets[ProviderType.EthereumOASIS]?.[0];

// Get total balance
const totalBalance = unifiedWallet.totalBalance;
```

## Testing Checklist

- [ ] Generate unified wallet successfully
- [ ] All 5 chains have wallets created
- [ ] Mnemonic phrase is displayed correctly
- [ ] Confirmation checkbox works
- [ ] Wallets appear in wallet list after creation
- [ ] Can use wallets for transactions
- [ ] Total balance calculation is correct
- [ ] Error handling for failed wallet creation
- [ ] Back button navigation works
- [ ] Copy mnemonic to clipboard works

## Known Limitations

1. **Not True HD Wallet Yet**
   - Currently generates independent keypairs
   - Not using BIP44 derivation paths
   - Each chain has separate keys (not derived from seed)

2. **Mnemonic Generation**
   - Uses simplified word list
   - Not BIP39 compliant yet
   - Should integrate proper library

3. **Recovery**
   - Cannot yet recover wallets from mnemonic
   - Need BIP44 implementation for this

## Migration Path

To make this a true HD wallet:

1. **Add BIP39 Library**
   ```bash
   npm install bip39 @scure/bip32 @scure/bip39
   ```

2. **Implement Derivation**
   ```typescript
   import { mnemonicToSeedSync } from '@scure/bip39';
   import { HDKey } from '@scure/bip32';
   
   const seed = mnemonicToSeedSync(mnemonic);
   const hdkey = HDKey.fromMasterSeed(seed);
   
   // Derive keys for each chain
   const solanaPath = "m/44'/501'/0'/0/0";
   const ethereumPath = "m/44'/60'/0'/0/0";
   // etc.
   ```

3. **Update Wallet Creation**
   - Use derived keys instead of generating new ones
   - Store derivation paths with wallets
   - Enable recovery from mnemonic

---

**Status**: ✅ Implemented (Basic)  
**Next**: BIP39/BIP44 Integration  
**Priority**: High (for production readiness)

