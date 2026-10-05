import { MigrationInterface, QueryRunner } from 'typeorm';

export class InitialSchema1738080000000 implements MigrationInterface {
  name = 'InitialSchema1738080000000';

  public async up(queryRunner: QueryRunner): Promise<void> {
    // Create users table
    await queryRunner.query(`
      CREATE TABLE "users" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "email" VARCHAR(255) NOT NULL,
        "password_hash" VARCHAR(255),
        "username" VARCHAR(255),
        "firstName" VARCHAR(255),
        "lastName" VARCHAR(255),
        "avatar_id" VARCHAR(255),
        "wallet_address_solana" VARCHAR(44),
        "wallet_address_ethereum" VARCHAR(42),
        "role" VARCHAR(20) NOT NULL DEFAULT 'user',
        "kyc_status" VARCHAR(20) NOT NULL DEFAULT 'pending',
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        "updated_at" TIMESTAMP NOT NULL DEFAULT now(),
        "last_login" TIMESTAMP,
        "is_active" BOOLEAN NOT NULL DEFAULT true,
        CONSTRAINT "UQ_users_email" UNIQUE ("email"),
        CONSTRAINT "UQ_users_avatar_id" UNIQUE ("avatar_id"),
        CONSTRAINT "PK_users" PRIMARY KEY ("id")
      )
    `);

    // Create tokenized_assets table
    await queryRunner.query(`
      CREATE TABLE "tokenized_assets" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "asset_id" VARCHAR(100) NOT NULL,
        "name" VARCHAR(255) NOT NULL,
        "symbol" VARCHAR(20) NOT NULL,
        "description" TEXT,
        "asset_class" VARCHAR(50) NOT NULL,
        "asset_type" VARCHAR(50),
        "total_supply" BIGINT NOT NULL,
        "decimals" INTEGER NOT NULL DEFAULT 0,
        "token_standard" VARCHAR(20),
        "blockchain" VARCHAR(20) NOT NULL,
        "network" VARCHAR(20) NOT NULL DEFAULT 'devnet',
        "contract_address" VARCHAR(255),
        "mint_address" VARCHAR(255),
        "total_value_usd" DECIMAL(18,2),
        "price_per_token_usd" DECIMAL(18,2),
        "metadata_uri" TEXT,
        "image_uri" TEXT,
        "legal_documents_uri" TEXT,
        "status" VARCHAR(20) NOT NULL DEFAULT 'draft',
        "issuer_id" uuid NOT NULL,
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        "updated_at" TIMESTAMP NOT NULL DEFAULT now(),
        "listed_at" TIMESTAMP,
        "metadata" jsonb,
        CONSTRAINT "UQ_tokenized_assets_asset_id" UNIQUE ("asset_id"),
        CONSTRAINT "PK_tokenized_assets" PRIMARY KEY ("id")
      )
    `);

    // Create orders table
    await queryRunner.query(`
      CREATE TABLE "orders" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "order_id" VARCHAR(100) NOT NULL,
        "user_id" uuid NOT NULL,
        "asset_id" uuid NOT NULL,
        "order_type" VARCHAR(20) NOT NULL,
        "order_status" VARCHAR(20) NOT NULL DEFAULT 'pending',
        "price_per_token_usd" DECIMAL(18,2) NOT NULL,
        "quantity" BIGINT NOT NULL,
        "total_value_usd" DECIMAL(18,2) NOT NULL,
        "filled_quantity" BIGINT NOT NULL DEFAULT 0,
        "remaining_quantity" BIGINT NOT NULL,
        "expires_at" TIMESTAMP,
        "blockchain" VARCHAR(20) NOT NULL,
        "transaction_hash" VARCHAR(255),
        "is_market_order" BOOLEAN NOT NULL DEFAULT false,
        "is_limit_order" BOOLEAN NOT NULL DEFAULT true,
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        "updated_at" TIMESTAMP NOT NULL DEFAULT now(),
        "filled_at" TIMESTAMP,
        CONSTRAINT "UQ_orders_order_id" UNIQUE ("order_id"),
        CONSTRAINT "PK_orders" PRIMARY KEY ("id")
      )
    `);

    // Create trades table
    await queryRunner.query(`
      CREATE TABLE "trades" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "trade_id" VARCHAR(100) NOT NULL,
        "buyer_id" uuid NOT NULL,
        "seller_id" uuid NOT NULL,
        "asset_id" uuid NOT NULL,
        "buy_order_id" uuid,
        "sell_order_id" uuid,
        "quantity" BIGINT NOT NULL,
        "price_per_token_usd" DECIMAL(18,2) NOT NULL,
        "total_value_usd" DECIMAL(18,2) NOT NULL,
        "platform_fee_usd" DECIMAL(18,2) NOT NULL DEFAULT 0,
        "platform_fee_percentage" DECIMAL(5,2) NOT NULL DEFAULT 0,
        "blockchain" VARCHAR(20) NOT NULL,
        "transaction_hash" VARCHAR(255) NOT NULL,
        "block_number" BIGINT,
        "executed_at" TIMESTAMP NOT NULL DEFAULT now(),
        "confirmed_at" TIMESTAMP,
        "status" VARCHAR(20) NOT NULL DEFAULT 'pending',
        "settlement_status" VARCHAR(20) NOT NULL DEFAULT 'pending',
        CONSTRAINT "UQ_trades_trade_id" UNIQUE ("trade_id"),
        CONSTRAINT "PK_trades" PRIMARY KEY ("id")
      )
    `);

    // Create user_balances table
    await queryRunner.query(`
      CREATE TABLE "user_balances" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "user_id" uuid NOT NULL,
        "asset_id" uuid NOT NULL,
        "balance" BIGINT NOT NULL DEFAULT 0,
        "available_balance" BIGINT NOT NULL DEFAULT 0,
        "locked_balance" BIGINT NOT NULL DEFAULT 0,
        "blockchain" VARCHAR(20) NOT NULL,
        "on_chain_balance" BIGINT,
        "last_synced_at" TIMESTAMP,
        "updated_at" TIMESTAMP NOT NULL DEFAULT now(),
        CONSTRAINT "UQ_user_balances_user_asset" UNIQUE ("user_id", "asset_id"),
        CONSTRAINT "PK_user_balances" PRIMARY KEY ("id")
      )
    `);

    // Create transactions table
    await queryRunner.query(`
      CREATE TABLE "transactions" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "transaction_id" VARCHAR(100) NOT NULL,
        "user_id" uuid NOT NULL,
        "asset_id" uuid NOT NULL,
        "transaction_type" VARCHAR(20) NOT NULL,
        "status" VARCHAR(20) NOT NULL DEFAULT 'pending',
        "amount" BIGINT NOT NULL,
        "amount_usd" DECIMAL(18,2),
        "blockchain" VARCHAR(20) NOT NULL,
        "transaction_hash" VARCHAR(255),
        "from_address" VARCHAR(255),
        "to_address" VARCHAR(255),
        "block_number" BIGINT,
        "created_at" TIMESTAMP NOT NULL DEFAULT now(),
        "confirmed_at" TIMESTAMP,
        "metadata" jsonb,
        CONSTRAINT "UQ_transactions_transaction_id" UNIQUE ("transaction_id"),
        CONSTRAINT "PK_transactions" PRIMARY KEY ("id")
      )
    `);

    // Create order_book_snapshots table
    await queryRunner.query(`
      CREATE TABLE "order_book_snapshots" (
        "id" uuid NOT NULL DEFAULT gen_random_uuid(),
        "asset_id" uuid NOT NULL,
        "best_bid_price" DECIMAL(18,2),
        "best_bid_quantity" BIGINT,
        "best_ask_price" DECIMAL(18,2),
        "best_ask_quantity" BIGINT,
        "last_trade_price" DECIMAL(18,2),
        "volume_24h" BIGINT,
        "high_24h" DECIMAL(18,2),
        "low_24h" DECIMAL(18,2),
        "snapshot_at" TIMESTAMP NOT NULL DEFAULT now(),
        CONSTRAINT "PK_order_book_snapshots" PRIMARY KEY ("id")
      )
    `);

    // Add foreign key constraints
    await queryRunner.query(`
      ALTER TABLE "tokenized_assets"
      ADD CONSTRAINT "FK_tokenized_assets_issuer"
      FOREIGN KEY ("issuer_id") REFERENCES "users"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "orders"
      ADD CONSTRAINT "FK_orders_user"
      FOREIGN KEY ("user_id") REFERENCES "users"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "orders"
      ADD CONSTRAINT "FK_orders_asset"
      FOREIGN KEY ("asset_id") REFERENCES "tokenized_assets"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "trades"
      ADD CONSTRAINT "FK_trades_buyer"
      FOREIGN KEY ("buyer_id") REFERENCES "users"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "trades"
      ADD CONSTRAINT "FK_trades_seller"
      FOREIGN KEY ("seller_id") REFERENCES "users"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "trades"
      ADD CONSTRAINT "FK_trades_asset"
      FOREIGN KEY ("asset_id") REFERENCES "tokenized_assets"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "trades"
      ADD CONSTRAINT "FK_trades_buy_order"
      FOREIGN KEY ("buy_order_id") REFERENCES "orders"("id")
      ON DELETE SET NULL ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "trades"
      ADD CONSTRAINT "FK_trades_sell_order"
      FOREIGN KEY ("sell_order_id") REFERENCES "orders"("id")
      ON DELETE SET NULL ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "user_balances"
      ADD CONSTRAINT "FK_user_balances_user"
      FOREIGN KEY ("user_id") REFERENCES "users"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "user_balances"
      ADD CONSTRAINT "FK_user_balances_asset"
      FOREIGN KEY ("asset_id") REFERENCES "tokenized_assets"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "transactions"
      ADD CONSTRAINT "FK_transactions_user"
      FOREIGN KEY ("user_id") REFERENCES "users"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "transactions"
      ADD CONSTRAINT "FK_transactions_asset"
      FOREIGN KEY ("asset_id") REFERENCES "tokenized_assets"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    await queryRunner.query(`
      ALTER TABLE "order_book_snapshots"
      ADD CONSTRAINT "FK_order_book_snapshots_asset"
      FOREIGN KEY ("asset_id") REFERENCES "tokenized_assets"("id")
      ON DELETE NO ACTION ON UPDATE NO ACTION
    `);

    // Create indexes for users table
    await queryRunner.query(`
      CREATE INDEX "idx_users_email" ON "users"("email")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_users_wallet_solana" ON "users"("wallet_address_solana")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_users_wallet_ethereum" ON "users"("wallet_address_ethereum")
    `);

    // Create indexes for tokenized_assets table
    await queryRunner.query(`
      CREATE INDEX "idx_assets_asset_id" ON "tokenized_assets"("asset_id")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_assets_status" ON "tokenized_assets"("status")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_assets_blockchain" ON "tokenized_assets"("blockchain")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_assets_contract_address" ON "tokenized_assets"("contract_address")
    `);

    // Create indexes for orders table
    await queryRunner.query(`
      CREATE INDEX "idx_orders_user_id" ON "orders"("user_id")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_orders_asset_id" ON "orders"("asset_id")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_orders_status" ON "orders"("order_status")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_orders_type_status" ON "orders"("order_type", "order_status")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_orders_created_at" ON "orders"("created_at" DESC)
    `);

    // Create indexes for trades table
    await queryRunner.query(`
      CREATE INDEX "idx_trades_buyer_id" ON "trades"("buyer_id")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_trades_seller_id" ON "trades"("seller_id")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_trades_asset_id" ON "trades"("asset_id")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_trades_executed_at" ON "trades"("executed_at" DESC)
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_trades_transaction_hash" ON "trades"("transaction_hash")
    `);

    // Create indexes for user_balances table
    await queryRunner.query(`
      CREATE INDEX "idx_balances_user_id" ON "user_balances"("user_id")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_balances_asset_id" ON "user_balances"("asset_id")
    `);

    // Create indexes for transactions table
    await queryRunner.query(`
      CREATE INDEX "idx_transactions_user_id" ON "transactions"("user_id")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_transactions_status" ON "transactions"("status")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_transactions_type" ON "transactions"("transaction_type")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_transactions_transaction_hash" ON "transactions"("transaction_hash")
    `);

    // Create indexes for order_book_snapshots table
    await queryRunner.query(`
      CREATE INDEX "idx_orderbook_asset_id" ON "order_book_snapshots"("asset_id")
    `);

    await queryRunner.query(`
      CREATE INDEX "idx_orderbook_snapshot_at" ON "order_book_snapshots"("snapshot_at" DESC)
    `);
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    // Drop indexes
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_orderbook_snapshot_at"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_orderbook_asset_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_transactions_transaction_hash"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_transactions_type"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_transactions_status"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_transactions_user_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_balances_asset_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_balances_user_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_trades_transaction_hash"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_trades_executed_at"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_trades_asset_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_trades_seller_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_trades_buyer_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_orders_created_at"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_orders_type_status"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_orders_status"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_orders_asset_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_orders_user_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_assets_contract_address"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_assets_blockchain"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_assets_status"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_assets_asset_id"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_users_wallet_ethereum"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_users_wallet_solana"`);
    await queryRunner.query(`DROP INDEX IF EXISTS "idx_users_email"`);

    // Drop foreign key constraints
    await queryRunner.query(`ALTER TABLE "order_book_snapshots" DROP CONSTRAINT IF EXISTS "FK_order_book_snapshots_asset"`);
    await queryRunner.query(`ALTER TABLE "transactions" DROP CONSTRAINT IF EXISTS "FK_transactions_asset"`);
    await queryRunner.query(`ALTER TABLE "transactions" DROP CONSTRAINT IF EXISTS "FK_transactions_user"`);
    await queryRunner.query(`ALTER TABLE "user_balances" DROP CONSTRAINT IF EXISTS "FK_user_balances_asset"`);
    await queryRunner.query(`ALTER TABLE "user_balances" DROP CONSTRAINT IF EXISTS "FK_user_balances_user"`);
    await queryRunner.query(`ALTER TABLE "trades" DROP CONSTRAINT IF EXISTS "FK_trades_sell_order"`);
    await queryRunner.query(`ALTER TABLE "trades" DROP CONSTRAINT IF EXISTS "FK_trades_buy_order"`);
    await queryRunner.query(`ALTER TABLE "trades" DROP CONSTRAINT IF EXISTS "FK_trades_asset"`);
    await queryRunner.query(`ALTER TABLE "trades" DROP CONSTRAINT IF EXISTS "FK_trades_seller"`);
    await queryRunner.query(`ALTER TABLE "trades" DROP CONSTRAINT IF EXISTS "FK_trades_buyer"`);
    await queryRunner.query(`ALTER TABLE "orders" DROP CONSTRAINT IF EXISTS "FK_orders_asset"`);
    await queryRunner.query(`ALTER TABLE "orders" DROP CONSTRAINT IF EXISTS "FK_orders_user"`);
    await queryRunner.query(`ALTER TABLE "tokenized_assets" DROP CONSTRAINT IF EXISTS "FK_tokenized_assets_issuer"`);

    // Drop tables (in reverse order of dependencies)
    await queryRunner.query(`DROP TABLE IF EXISTS "order_book_snapshots"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "transactions"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "user_balances"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "trades"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "orders"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "tokenized_assets"`);
    await queryRunner.query(`DROP TABLE IF EXISTS "users"`);
  }
}












