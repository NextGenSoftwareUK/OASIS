# Database Schema Documentation

**Project:** Pangea Markets RWA Trading Platform  
**Database:** PostgreSQL  
**ORM:** TypeORM  
**Last Updated:** 2025-01-27

---

## Overview

This document describes the complete database schema for Pangea Markets, including all tables, relationships, indexes, and constraints.

---

## Table Structure

### 1. Users Table (`users`)

Stores user accounts and authentication information.

**Columns:**
- `id` (UUID, Primary Key) - Unique identifier
- `email` (VARCHAR(255), UNIQUE, NOT NULL, INDEXED) - User email address
- `password_hash` (VARCHAR(255), NULLABLE) - Hashed password (nullable if using OASIS auth only)
- `username` (VARCHAR(255), NULLABLE) - Username
- `firstName` (VARCHAR(255), NULLABLE) - First name
- `lastName` (VARCHAR(255), NULLABLE) - Last name
- `avatar_id` (VARCHAR(255), UNIQUE, NULLABLE) - Link to OASIS Avatar ID
- `wallet_address_solana` (VARCHAR(44), NULLABLE, INDEXED) - Solana wallet address
- `wallet_address_ethereum` (VARCHAR(42), NULLABLE, INDEXED) - Ethereum wallet address
- `role` (VARCHAR(20), DEFAULT 'user') - User role: 'user', 'admin', 'moderator'
- `kyc_status` (VARCHAR(20), DEFAULT 'pending') - KYC status: 'pending', 'approved', 'rejected'
- `created_at` (TIMESTAMP, DEFAULT NOW()) - Account creation timestamp
- `updated_at` (TIMESTAMP, DEFAULT NOW()) - Last update timestamp
- `last_login` (TIMESTAMP, NULLABLE) - Last login timestamp
- `is_active` (BOOLEAN, DEFAULT true) - Account active status

**Indexes:**
- `idx_users_email` on `email`
- `idx_users_wallet_solana` on `wallet_address_solana`
- `idx_users_wallet_ethereum` on `wallet_address_ethereum`

**Relationships:**
- One-to-Many with `tokenized_assets` (as issuer)
- One-to-Many with `orders`
- One-to-Many with `trades` (as buyer and seller)
- One-to-Many with `user_balances`
- One-to-Many with `transactions`

---

### 2. Tokenized Assets Table (`tokenized_assets`)

Stores tokenized Real World Assets (RWA) with metadata and blockchain information.

**Columns:**
- `id` (UUID, Primary Key) - Unique identifier
- `asset_id` (VARCHAR(100), UNIQUE, NOT NULL, INDEXED) - Asset identifier (e.g., "BHE-2025-001")
- `name` (VARCHAR(255), NOT NULL) - Asset name
- `symbol` (VARCHAR(20), NOT NULL) - Asset symbol
- `description` (TEXT, NULLABLE) - Asset description
- `asset_class` (VARCHAR(50), NOT NULL) - Asset class: 'real_estate', 'art', 'commodities', etc.
- `asset_type` (VARCHAR(50), NULLABLE) - Asset type: 'residential', 'commercial', etc.
- `total_supply` (BIGINT, NOT NULL) - Total token supply
- `decimals` (INTEGER, DEFAULT 0) - Token decimals
- `token_standard` (VARCHAR(20), NULLABLE) - Token standard: 'SPL', 'ERC-721', 'ERC-1155', 'UAT'
- `blockchain` (VARCHAR(20), NOT NULL, INDEXED) - Blockchain: 'solana', 'ethereum', 'radix'
- `network` (VARCHAR(20), DEFAULT 'devnet') - Network: 'devnet', 'testnet', 'mainnet'
- `contract_address` (VARCHAR(255), NULLABLE, INDEXED) - Smart contract address
- `mint_address` (VARCHAR(255), NULLABLE) - Solana mint address
- `total_value_usd` (DECIMAL(18,2), NULLABLE) - Total asset value in USD
- `price_per_token_usd` (DECIMAL(18,2), NULLABLE) - Price per token in USD
- `metadata_uri` (TEXT, NULLABLE) - IPFS URI for metadata
- `image_uri` (TEXT, NULLABLE) - Asset image URI
- `legal_documents_uri` (TEXT, NULLABLE) - Legal documents URI
- `status` (VARCHAR(20), DEFAULT 'draft', INDEXED) - Status: 'draft', 'listed', 'trading', 'closed'
- `issuer_id` (UUID, NOT NULL, Foreign Key) - Reference to users table (issuer)
- `created_at` (TIMESTAMP, DEFAULT NOW()) - Creation timestamp
- `updated_at` (TIMESTAMP, DEFAULT NOW()) - Last update timestamp
- `listed_at` (TIMESTAMP, NULLABLE) - Listing timestamp
- `metadata` (JSONB, NULLABLE) - Additional JSON metadata

**Indexes:**
- `idx_assets_asset_id` on `asset_id`
- `idx_assets_status` on `status`
- `idx_assets_blockchain` on `blockchain`
- `idx_assets_contract_address` on `contract_address`

**Relationships:**
- Many-to-One with `users` (issuer)
- One-to-Many with `orders`
- One-to-Many with `trades`
- One-to-Many with `user_balances`
- One-to-Many with `transactions`
- One-to-Many with `order_book_snapshots`

---

### 3. Orders Table (`orders`)

Stores buy and sell orders for tokenized assets.

**Columns:**
- `id` (UUID, Primary Key) - Unique identifier
- `order_id` (VARCHAR(100), UNIQUE, NOT NULL) - Order identifier
- `user_id` (UUID, NOT NULL, Foreign Key, INDEXED) - Reference to users table
- `asset_id` (UUID, NOT NULL, Foreign Key, INDEXED) - Reference to tokenized_assets table
- `order_type` (VARCHAR(20), NOT NULL) - Order type: 'buy', 'sell'
- `order_status` (VARCHAR(20), DEFAULT 'pending', INDEXED) - Status: 'pending', 'open', 'filled', 'cancelled', 'expired'
- `price_per_token_usd` (DECIMAL(18,2), NOT NULL) - Price per token in USD
- `quantity` (BIGINT, NOT NULL) - Order quantity (raw units)
- `total_value_usd` (DECIMAL(18,2), NOT NULL) - Total order value in USD
- `filled_quantity` (BIGINT, DEFAULT 0) - Filled quantity
- `remaining_quantity` (BIGINT, NOT NULL) - Remaining quantity
- `expires_at` (TIMESTAMP, NULLABLE) - Order expiration timestamp
- `blockchain` (VARCHAR(20), NOT NULL) - Blockchain: 'solana', 'ethereum'
- `transaction_hash` (VARCHAR(255), NULLABLE) - On-chain transaction hash
- `is_market_order` (BOOLEAN, DEFAULT false) - Market order flag
- `is_limit_order` (BOOLEAN, DEFAULT true) - Limit order flag
- `created_at` (TIMESTAMP, DEFAULT NOW(), INDEXED DESC) - Order creation timestamp
- `updated_at` (TIMESTAMP, DEFAULT NOW()) - Last update timestamp
- `filled_at` (TIMESTAMP, NULLABLE) - Order fill timestamp

**Indexes:**
- `idx_orders_user_id` on `user_id`
- `idx_orders_asset_id` on `asset_id`
- `idx_orders_status` on `order_status`
- `idx_orders_type_status` on `order_type`, `order_status` (composite)
- `idx_orders_created_at` on `created_at` (DESC)

**Relationships:**
- Many-to-One with `users`
- Many-to-One with `tokenized_assets`
- One-to-Many with `trades` (as buy_order_id and sell_order_id)

---

### 4. Trades Table (`trades`)

Stores executed trades between orders.

**Columns:**
- `id` (UUID, Primary Key) - Unique identifier
- `trade_id` (VARCHAR(100), UNIQUE, NOT NULL) - Trade identifier
- `buyer_id` (UUID, NOT NULL, Foreign Key, INDEXED) - Reference to users table (buyer)
- `seller_id` (UUID, NOT NULL, Foreign Key, INDEXED) - Reference to users table (seller)
- `asset_id` (UUID, NOT NULL, Foreign Key, INDEXED) - Reference to tokenized_assets table
- `buy_order_id` (UUID, NULLABLE, Foreign Key) - Reference to orders table (buy order)
- `sell_order_id` (UUID, NULLABLE, Foreign Key) - Reference to orders table (sell order)
- `quantity` (BIGINT, NOT NULL) - Trade quantity (raw units)
- `price_per_token_usd` (DECIMAL(18,2), NOT NULL) - Price per token in USD
- `total_value_usd` (DECIMAL(18,2), NOT NULL) - Total trade value in USD
- `platform_fee_usd` (DECIMAL(18,2), DEFAULT 0) - Platform fee in USD
- `platform_fee_percentage` (DECIMAL(5,2), DEFAULT 0) - Platform fee percentage
- `blockchain` (VARCHAR(20), NOT NULL) - Blockchain: 'solana', 'ethereum'
- `transaction_hash` (VARCHAR(255), NOT NULL, INDEXED) - On-chain transaction hash
- `block_number` (BIGINT, NULLABLE) - Block number
- `executed_at` (TIMESTAMP, DEFAULT NOW(), INDEXED DESC) - Trade execution timestamp
- `confirmed_at` (TIMESTAMP, NULLABLE) - Transaction confirmation timestamp
- `status` (VARCHAR(20), DEFAULT 'pending') - Status: 'pending', 'confirmed', 'failed'
- `settlement_status` (VARCHAR(20), DEFAULT 'pending') - Settlement status: 'pending', 'settled', 'failed'

**Indexes:**
- `idx_trades_buyer_id` on `buyer_id`
- `idx_trades_seller_id` on `seller_id`
- `idx_trades_asset_id` on `asset_id`
- `idx_trades_executed_at` on `executed_at` (DESC)
- `idx_trades_transaction_hash` on `transaction_hash`

**Relationships:**
- Many-to-One with `users` (as buyer)
- Many-to-One with `users` (as seller)
- Many-to-One with `tokenized_assets`
- Many-to-One with `orders` (as buy_order)
- Many-to-One with `orders` (as sell_order)

---

### 5. User Balances Table (`user_balances`)

Stores token balances per user per asset.

**Columns:**
- `id` (UUID, Primary Key) - Unique identifier
- `user_id` (UUID, NOT NULL, Foreign Key, INDEXED) - Reference to users table
- `asset_id` (UUID, NOT NULL, Foreign Key, INDEXED) - Reference to tokenized_assets table
- `balance` (BIGINT, DEFAULT 0) - Total token balance (raw units)
- `available_balance` (BIGINT, DEFAULT 0) - Available balance for trading
- `locked_balance` (BIGINT, DEFAULT 0) - Balance locked in orders
- `blockchain` (VARCHAR(20), NOT NULL) - Blockchain: 'solana', 'ethereum'
- `on_chain_balance` (BIGINT, NULLABLE) - Verified on-chain balance
- `last_synced_at` (TIMESTAMP, NULLABLE) - Last synchronization timestamp
- `updated_at` (TIMESTAMP, DEFAULT NOW()) - Last update timestamp

**Constraints:**
- UNIQUE constraint on (`user_id`, `asset_id`) - One balance record per user per asset

**Indexes:**
- `idx_balances_user_id` on `user_id`
- `idx_balances_asset_id` on `asset_id`

**Relationships:**
- Many-to-One with `users`
- Many-to-One with `tokenized_assets`

---

### 6. Transactions Table (`transactions`)

Stores deposit and withdrawal transactions.

**Columns:**
- `id` (UUID, Primary Key) - Unique identifier
- `transaction_id` (VARCHAR(100), UNIQUE, NOT NULL) - Transaction identifier
- `user_id` (UUID, NOT NULL, Foreign Key, INDEXED) - Reference to users table
- `asset_id` (UUID, NOT NULL, Foreign Key) - Reference to tokenized_assets table
- `transaction_type` (VARCHAR(20), NOT NULL, INDEXED) - Type: 'deposit', 'withdrawal'
- `status` (VARCHAR(20), DEFAULT 'pending', INDEXED) - Status: 'pending', 'processing', 'completed', 'failed'
- `amount` (BIGINT, NOT NULL) - Transaction amount (raw units)
- `amount_usd` (DECIMAL(18,2), NULLABLE) - Transaction amount in USD
- `blockchain` (VARCHAR(20), NOT NULL) - Blockchain: 'solana', 'ethereum'
- `transaction_hash` (VARCHAR(255), NULLABLE, INDEXED) - On-chain transaction hash
- `from_address` (VARCHAR(255), NULLABLE) - Source address
- `to_address` (VARCHAR(255), NULLABLE) - Destination address
- `block_number` (BIGINT, NULLABLE) - Block number
- `created_at` (TIMESTAMP, DEFAULT NOW()) - Transaction creation timestamp
- `confirmed_at` (TIMESTAMP, NULLABLE) - Transaction confirmation timestamp
- `metadata` (JSONB, NULLABLE) - Additional JSON metadata

**Indexes:**
- `idx_transactions_user_id` on `user_id`
- `idx_transactions_status` on `status`
- `idx_transactions_type` on `transaction_type`
- `idx_transactions_transaction_hash` on `transaction_hash`

**Relationships:**
- Many-to-One with `users`
- Many-to-One with `tokenized_assets`

---

### 7. Order Book Snapshots Table (`order_book_snapshots`)

Stores real-time order book snapshots for market data.

**Columns:**
- `id` (UUID, Primary Key) - Unique identifier
- `asset_id` (UUID, NOT NULL, Foreign Key, INDEXED) - Reference to tokenized_assets table
- `best_bid_price` (DECIMAL(18,2), NULLABLE) - Best bid price
- `best_bid_quantity` (BIGINT, NULLABLE) - Best bid quantity
- `best_ask_price` (DECIMAL(18,2), NULLABLE) - Best ask price
- `best_ask_quantity` (BIGINT, NULLABLE) - Best ask quantity
- `last_trade_price` (DECIMAL(18,2), NULLABLE) - Last trade price
- `volume_24h` (BIGINT, NULLABLE) - 24-hour volume
- `high_24h` (DECIMAL(18,2), NULLABLE) - 24-hour high price
- `low_24h` (DECIMAL(18,2), NULLABLE) - 24-hour low price
- `snapshot_at` (TIMESTAMP, DEFAULT NOW(), INDEXED DESC) - Snapshot timestamp

**Indexes:**
- `idx_orderbook_asset_id` on `asset_id`
- `idx_orderbook_snapshot_at` on `snapshot_at` (DESC)

**Relationships:**
- Many-to-One with `tokenized_assets`

---

## Entity Relationship Diagram

```
users
  ├── tokenized_assets (issuer_id)
  ├── orders (user_id)
  ├── trades (buyer_id, seller_id)
  ├── user_balances (user_id)
  └── transactions (user_id)

tokenized_assets
  ├── orders (asset_id)
  ├── trades (asset_id)
  ├── user_balances (asset_id)
  ├── transactions (asset_id)
  └── order_book_snapshots (asset_id)

orders
  ├── trades (buy_order_id, sell_order_id)
```

---

## Data Types and Constraints

### UUID Primary Keys
All tables use UUID primary keys generated with `gen_random_uuid()` for better distributed system support.

### Decimal Precision
- USD amounts: `DECIMAL(18, 2)` - 18 total digits, 2 decimal places
- Platform fees: `DECIMAL(5, 2)` - 5 total digits, 2 decimal places
- This prevents floating-point precision errors in financial calculations

### BigInt for Token Amounts
All token quantities use `BIGINT` to store raw units (before applying decimals), ensuring precision for large token supplies.

### JSONB for Metadata
Flexible metadata storage using PostgreSQL's JSONB type for efficient querying and indexing.

### Timestamps
- `created_at` - Auto-generated on creation
- `updated_at` - Auto-updated on modification
- `TIMESTAMP` with timezone support

### Foreign Key Constraints
All foreign key relationships are enforced with constraints, ensuring referential integrity.

### Unique Constraints
- `users.email` - Unique email addresses
- `users.avatar_id` - Unique OASIS Avatar IDs
- `tokenized_assets.asset_id` - Unique asset identifiers
- `orders.order_id` - Unique order identifiers
- `trades.trade_id` - Unique trade identifiers
- `transactions.transaction_id` - Unique transaction identifiers
- `user_balances(user_id, asset_id)` - One balance per user per asset

---

## Indexes

All indexes are designed to optimize frequently queried columns:

1. **User lookups:** email, wallet addresses
2. **Asset lookups:** asset_id, status, blockchain, contract_address
3. **Order queries:** user_id, asset_id, status, type+status composite, created_at
4. **Trade queries:** buyer_id, seller_id, asset_id, executed_at, transaction_hash
5. **Balance queries:** user_id, asset_id
6. **Transaction queries:** user_id, status, type, transaction_hash
7. **Order book queries:** asset_id, snapshot_at

---

## Migration Management

### Running Migrations

```bash
# Generate a new migration (after entity changes)
npm run migration:generate -- migrations/MigrationName

# Run pending migrations
npm run migration:run

# Revert last migration
npm run migration:revert
```

### Initial Migration

The initial migration (`1738080000000-InitialSchema.ts`) creates all tables, indexes, and foreign key constraints.

---

## TypeORM Entities

All entities are located in their respective module directories:
- `src/users/entities/user.entity.ts`
- `src/users/entities/user-balance.entity.ts`
- `src/assets/entities/tokenized-asset.entity.ts`
- `src/orders/entities/order.entity.ts`
- `src/orders/entities/order-book-snapshot.entity.ts`
- `src/trades/entities/trade.entity.ts`
- `src/transactions/entities/transaction.entity.ts`

---

## Notes

1. **Password Hash:** The `password_hash` field in the users table is nullable to support OASIS authentication integration. If using OASIS auth exclusively, this field may remain null.

2. **Balance Tracking:** The `user_balances` table maintains both on-chain and off-chain balances. The `on_chain_balance` field should be synced periodically with blockchain state.

3. **Order Matching:** The order matching engine (implemented separately) reads from the `orders` table and writes to the `trades` table.

4. **Real-time Data:** The `order_book_snapshots` table is optional and used for caching real-time market data. It can be populated via background jobs or WebSocket updates.

5. **Transaction Tracking:** The `transactions` table tracks both deposits and withdrawals, with status tracking for pending, processing, completed, and failed states.

---

## References

- Task Brief: `task-briefs/02-database-schema.md`
- Implementation Plan: `../IMPLEMENTATION_PLAN.md` Section 2
- TypeORM Documentation: https://typeorm.io/

---

**Last Updated:** 2025-01-27  
**Schema Version:** 1.0.0












