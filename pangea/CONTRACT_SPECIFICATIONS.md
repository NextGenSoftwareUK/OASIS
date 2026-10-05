# Smart Contract Specifications for Pangea Markets

This document outlines the smart contract specifications needed for the RWA trading platform.

---

## Contract Types Required

### 1. RWA Token Contract (Solana Anchor)

**Purpose:** Represent tokenized real-world assets

**JSON Specification:**
```json
{
  "contract_type": "rwa_token",
  "blockchain": "solana",
  "language": "Rust",
  "framework": "Anchor",
  "spec": {
    "name": "{{asset_name}}",
    "symbol": "{{asset_symbol}}",
    "total_supply": "{{total_supply}}",
    "decimals": 0,
    "metadata_uri": "{{ipfs_uri}}",
    "features": [
      "mint",
      "burn",
      "transfer",
      "freeze",
      "metadata_update"
    ],
    "authority": "{{issuer_wallet}}",
    "freeze_authority": "{{issuer_wallet}}"
  }
}
```

**Key Functions:**
- `mint()` - Mint tokens to address
- `burn()` - Burn tokens
- `transfer()` - Transfer tokens
- `freeze()` - Freeze account (if needed)
- `get_balance()` - Get token balance

---

### 2. Order Book Contract (Solana Anchor)

**Purpose:** Manage buy/sell orders on-chain

**JSON Specification:**
```json
{
  "contract_type": "order_book",
  "blockchain": "solana",
  "language": "Rust",
  "framework": "Anchor",
  "spec": {
    "features": [
      "create_buy_order",
      "create_sell_order",
      "cancel_order",
      "match_orders",
      "get_order_book",
      "get_user_orders",
      "get_order_by_id"
    ],
    "order_types": [
      "limit",
      "market"
    ],
    "matching_algorithm": "price_time_priority"
  }
}
```

**Key Functions:**
- `create_order(order_type, price, quantity, expires_at)` - Create order
- `cancel_order(order_id)` - Cancel order
- `match_orders()` - Match compatible orders
- `get_order_book(asset_id)` - Get current order book
- `get_user_orders(user_address)` - Get user's orders

**Order Structure:**
```rust
pub struct Order {
    pub id: Pubkey,
    pub user: Pubkey,
    pub asset_mint: Pubkey,
    pub order_type: OrderType, // Buy or Sell
    pub price: u64,
    pub quantity: u64,
    pub filled_quantity: u64,
    pub status: OrderStatus,
    pub created_at: i64,
    pub expires_at: Option<i64>,
}
```

---

### 3. Trade Execution Contract (Solana Anchor)

**Purpose:** Execute trades between matched orders

**JSON Specification:**
```json
{
  "contract_type": "trade_execution",
  "blockchain": "solana",
  "language": "Rust",
  "framework": "Anchor",
  "spec": {
    "features": [
      "execute_trade",
      "settle_trade",
      "get_trade_history",
      "calculate_fees"
    ],
    "fee_structure": {
      "platform_fee_percentage": 0.01,
      "fee_recipient": "{{platform_wallet}}"
    }
  }
}
```

**Key Functions:**
- `execute_trade(buy_order_id, sell_order_id)` - Execute trade
- `settle_trade(trade_id)` - Finalize settlement
- `get_trade_history(asset_id, limit)` - Get trade history
- `calculate_fees(trade_amount)` - Calculate platform fees

**Trade Structure:**
```rust
pub struct Trade {
    pub id: Pubkey,
    pub buyer: Pubkey,
    pub seller: Pubkey,
    pub asset_mint: Pubkey,
    pub quantity: u64,
    pub price: u64,
    pub total_value: u64,
    pub platform_fee: u64,
    pub executed_at: i64,
    pub transaction_hash: String,
}
```

---

### 4. Vault/Deposit Contract (Solana Anchor)

**Purpose:** Handle deposits and withdrawals

**JSON Specification:**
```json
{
  "contract_type": "vault",
  "blockchain": "solana",
  "language": "Rust",
  "framework": "Anchor",
  "spec": {
    "features": [
      "deposit",
      "withdraw",
      "get_balance",
      "transfer_to_order",
      "lock_balance",
      "unlock_balance"
    ],
    "supported_tokens": [
      "USDC",
      "SOL",
      "{{asset_tokens}}"
    ]
  }
}
```

**Key Functions:**
- `deposit(amount, token_mint)` - Deposit tokens
- `withdraw(amount, token_mint)` - Withdraw tokens
- `get_balance(user_address, token_mint)` - Get balance
- `lock_balance(amount, token_mint)` - Lock for order
- `unlock_balance(amount, token_mint)` - Unlock after order

---

## Contract Deployment Flow

### Step 1: Generate Contract
```bash
POST /api/v1/contracts/generate
{
  "Language": "Rust",
  "JsonFile": <contract-spec.json>
}
```

### Step 2: Compile Contract
```bash
POST /api/v1/contracts/compile
{
  "Language": "Rust",
  "Source": <generated-contract.zip>
}
```

### Step 3: Deploy Contract
```bash
POST /api/v1/contracts/deploy
{
  "Language": "Rust",
  "WalletKeypair": <keypair-file>,
  "CompiledContractFile": <program.so>
}
```

### Step 4: Store Contract Address
- Save contract address in database
- Link to asset/user as needed
- Update configuration

---

## Integration with Backend

### Contract Service Example

```typescript
// services/smart-contract.service.ts
import axios from 'axios';

class SmartContractService {
  private generatorApiUrl = 'http://localhost:5000/api/v1/contracts';
  
  async generateRwaToken(assetSpec: {
    name: string;
    symbol: string;
    totalSupply: number;
    metadataUri: string;
  }): Promise<string> {
    // 1. Create JSON spec
    const spec = {
      contract_type: 'rwa_token',
      blockchain: 'solana',
      language: 'Rust',
      framework: 'Anchor',
      spec: {
        name: assetSpec.name,
        symbol: assetSpec.symbol,
        total_supply: assetSpec.totalSupply,
        decimals: 0,
        metadata_uri: assetSpec.metadataUri,
        features: ['mint', 'burn', 'transfer', 'freeze']
      }
    };
    
    // 2. Generate
    const generateResponse = await axios.post(
      `${this.generatorApiUrl}/generate`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    
    // 3. Compile
    const compileResponse = await axios.post(
      `${this.generatorApiUrl}/compile`,
      compileFormData
    );
    
    // 4. Deploy
    const deployResponse = await axios.post(
      `${this.generatorApiUrl}/deploy`,
      deployFormData
    );
    
    // 5. Return contract address
    return deployResponse.data.contractAddress;
  }
  
  async deployOrderBook(): Promise<string> {
    // Similar flow for order book contract
  }
  
  async deployTradeExecution(): Promise<string> {
    // Similar flow for trade execution contract
  }
  
  async deployVault(): Promise<string> {
    // Similar flow for vault contract
  }
}
```

---

## Contract Interaction Service

```typescript
// services/blockchain.service.ts
import { Connection, PublicKey, Keypair } from '@solana/web3.js';
import { Program, AnchorProvider } from '@coral-xyz/anchor';

class BlockchainService {
  private connection: Connection;
  private program: Program;
  
  async getTokenBalance(
    walletAddress: string,
    mintAddress: string
  ): Promise<bigint> {
    // Query on-chain balance
  }
  
  async createOrder(
    orderData: {
      user: string;
      assetMint: string;
      orderType: 'buy' | 'sell';
      price: bigint;
      quantity: bigint;
    }
  ): Promise<string> {
    // Call order book contract
    // Return transaction signature
  }
  
  async executeTrade(
    buyOrderId: string,
    sellOrderId: string
  ): Promise<string> {
    // Call trade execution contract
    // Return transaction signature
  }
  
  async deposit(
    userAddress: string,
    amount: bigint,
    tokenMint: string
  ): Promise<string> {
    // Call vault contract deposit
    // Return transaction signature
  }
  
  async withdraw(
    userAddress: string,
    amount: bigint,
    tokenMint: string
  ): Promise<string> {
    // Call vault contract withdraw
    // Return transaction signature
  }
}
```

---

## Testing Strategy

### Unit Tests
- Contract generation
- Contract compilation
- Contract deployment

### Integration Tests
- Contract interaction
- Transaction submission
- Balance queries

### Testnet Testing
- Deploy to Solana devnet
- Test all contract functions
- Verify transaction confirmations
- Test error handling

---

## Security Considerations

1. **Access Control**
   - Only authorized users can create orders
   - Only order owners can cancel
   - Platform fees enforced on-chain

2. **Input Validation**
   - Validate all inputs before contract calls
   - Check balances before trades
   - Verify order ownership

3. **Error Handling**
   - Handle transaction failures
   - Retry logic for network issues
   - Fallback mechanisms

4. **Audit Trail**
   - Log all contract interactions
   - Store transaction hashes
   - Track contract state changes

---

## Next Steps

1. Create detailed JSON specifications for each contract
2. Test contract generation with SmartContractGenerator
3. Deploy test contracts to Solana devnet
4. Integrate contract addresses into backend
5. Implement contract interaction services

---

**Reference:**
- SmartContractGenerator: `/Volumes/Storage 4/OASIS_CLEAN/SmartContractGenerator`
- UAT Specification: `/Volumes/Storage 4/OASIS_CLEAN/UAT/UNIVERSAL_ASSET_TOKEN_SPECIFICATION.md`
- Solana Anchor Docs: https://www.anchor-lang.com/
