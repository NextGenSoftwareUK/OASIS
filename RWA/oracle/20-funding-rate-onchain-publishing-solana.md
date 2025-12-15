# Task 20: Funding Rate On-Chain Publishing (Solana)

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 1-2 weeks  
**Dependencies:** Task 19 (Funding Rate Calculation Service)

---

## 📋 Overview

Publish funding rates to Solana blockchain so perpetual DEXs can read them on-chain. Use Program Derived Addresses (PDAs) to store funding rate data efficiently.

---

## ✅ Objectives

1. Create Solana program (Anchor) for storing funding rates
2. Implement PDA-based storage for each symbol
3. Build service to publish funding rates on-chain
4. Handle transaction signing and submission
5. Provide on-chain data reading capabilities
6. Implement update mechanism (hourly updates)

---

## 🎯 Requirements

### 1. **Solana Program Structure (Anchor)**

```rust
// programs/rwa-oracle/src/lib.rs

use anchor_lang::prelude::*;

declare_id!("[PROGRAM_ID]");

#[program]
pub mod rwa_oracle {
    use super::*;

    pub fn initialize_funding_rate(
        ctx: Context<InitializeFundingRate>,
        symbol: String,
    ) -> Result<()> {
        let funding_rate = &mut ctx.accounts.funding_rate;
        funding_rate.symbol = symbol;
        funding_rate.authority = ctx.accounts.authority.key();
        funding_rate.bump = ctx.bumps.funding_rate;
        Ok(())
    }

    pub fn update_funding_rate(
        ctx: Context<UpdateFundingRate>,
        rate: i64, // Annualized rate in basis points (10000 = 100%)
        hourly_rate: i64, // Hourly rate in basis points
        mark_price: u64, // In smallest unit (e.g., for $150.25 = 15025000000)
        spot_price: u64,
        premium: i64, // Can be negative
        valid_until: i64, // Unix timestamp
    ) -> Result<()> {
        let funding_rate = &mut ctx.accounts.funding_rate;
        
        require!(
            ctx.accounts.authority.key() == funding_rate.authority,
            ErrorCode::Unauthorized
        );

        funding_rate.rate = rate;
        funding_rate.hourly_rate = hourly_rate;
        funding_rate.mark_price = mark_price;
        funding_rate.spot_price = spot_price;
        funding_rate.premium = premium;
        funding_rate.last_updated = Clock::get()?.unix_timestamp;
        funding_rate.valid_until = valid_until;

        Ok(())
    }
}

#[account]
pub struct FundingRate {
    pub symbol: String, // Max 10 chars
    pub authority: Pubkey,
    pub rate: i64, // Annualized rate in basis points
    pub hourly_rate: i64, // Hourly rate in basis points
    pub mark_price: u64,
    pub spot_price: u64,
    pub premium: i64,
    pub last_updated: i64, // Unix timestamp
    pub valid_until: i64, // Unix timestamp
    pub bump: u8,
}

impl FundingRate {
    pub const MAX_SIZE: usize = 10 + // symbol
        32 + // authority
        8 + // rate
        8 + // hourly_rate
        8 + // mark_price
        8 + // spot_price
        8 + // premium
        8 + // last_updated
        8 + // valid_until
        1; // bump
}

#[derive(Accounts)]
pub struct InitializeFundingRate<'info> {
    #[account(
        init,
        payer = authority,
        space = FundingRate::MAX_SIZE,
        seeds = [b"funding_rate", symbol.as_bytes()],
        bump
    )]
    pub funding_rate: Account<'info, FundingRate>,
    #[account(mut)]
    pub authority: Signer<'info>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct UpdateFundingRate<'info> {
    #[account(
        mut,
        seeds = [b"funding_rate", funding_rate.symbol.as_bytes()],
        bump = funding_rate.bump
    )]
    pub funding_rate: Account<'info, FundingRate>,
    pub authority: Signer<'info>,
}

#[error_code]
pub enum ErrorCode {
    #[msg("Unauthorized")]
    Unauthorized,
}
```

### 2. **C# Service Interface**

```csharp
public interface IOnChainFundingPublisher
{
    Task<string> PublishFundingRateAsync(string symbol, FundingRate rate);
    Task<List<string>> PublishBatchFundingRatesAsync(Dictionary<string, FundingRate> rates);
    Task<OnChainFundingRate?> GetOnChainFundingRateAsync(string symbol);
    Task<Dictionary<string, OnChainFundingRate>> GetOnChainFundingRatesAsync(List<string> symbols);
    Task<bool> InitializeFundingRateAccountAsync(string symbol);
    Task<bool> IsFundingRateAccountInitializedAsync(string symbol);
}

public class OnChainFundingRate
{
    public string Symbol { get; set; }
    public decimal Rate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal SpotPrice { get; set; }
    public decimal Premium { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime ValidUntil { get; set; }
    public string TransactionHash { get; set; }
    public string AccountAddress { get; set; }
}
```

### 3. **Solana Integration Setup**

#### Required NuGet Packages:
- `Solana.Unity.SDK` or `Solnet.Programs`
- `Solnet.Wallet`
- `Solnet.Rpc`
- `Anchor.Client` (if using Anchor)

#### Configuration:
```json
{
  "Solana": {
    "RpcUrl": "https://api.mainnet-beta.solana.com",
    "WalletPrivateKey": "[ENCRYPTED]",
    "ProgramId": "[PROGRAM_ID]",
    "Network": "mainnet-beta" // or "devnet" for testing
  }
}
```

### 4. **PDA Calculation**

For each symbol, derive PDA:
```
seeds = ["funding_rate", symbol_bytes]
PDA = findProgramDerivedAddress(seeds, programId)
```

Example for "AAPL":
```
seeds = [b"funding_rate", b"AAPL"]
PDA = DeriveAddress(seeds, programId)
```

### 5. **Transaction Building**

```csharp
public async Task<string> PublishFundingRateAsync(string symbol, FundingRate rate)
{
    // 1. Get or create funding rate account PDA
    var pda = await GetOrCreateFundingRateAccountAsync(symbol);
    
    // 2. Build update instruction
    var instruction = BuildUpdateFundingRateInstruction(
        pda,
        rate,
        AuthorityPublicKey
    );
    
    // 3. Build transaction
    var transaction = new Transaction();
    transaction.Add(instruction);
    
    // 4. Sign transaction
    var signedTransaction = await SignTransactionAsync(transaction);
    
    // 5. Send transaction
    var signature = await SendTransactionAsync(signedTransaction);
    
    // 6. Confirm transaction
    await ConfirmTransactionAsync(signature);
    
    return signature;
}
```

### 6. **Price Precision Handling**

Store prices with fixed precision (e.g., 8 decimals):
```
$150.25 → 15025000000 (multiply by 10^8)
```

When reading:
```
15025000000 → $150.25 (divide by 10^8)
```

### 7. **Rate Storage Format**

Store rates in basis points:
```
10% annualized → 1000 basis points
0.1% hourly → 10 basis points (0.1 * 100)

Or use fixed-point with decimals:
10% → 10000000 (10 * 10^6, 6 decimals)
```

---

## 📁 Files to Create

```
programs/rwa-oracle/
  ├── Cargo.toml
  ├── Xargo.toml
  └── src/
      └── lib.rs (Anchor program)

Infrastructure/Blockchain/Solana/
  ├── IOnChainFundingPublisher.cs
  ├── OnChainFundingPublisher.cs
  ├── SolanaConfig.cs
  ├── SolanaTransactionBuilder.cs
  └── Models/
      └── OnChainFundingRate.cs

Infrastructure/Blockchain/Solana/Anchor/
  ├── AnchorClient.cs (if using Anchor client)
  └── Idl/
      └── rwa_oracle.json (generated IDL)

API/Infrastructure/DI/CustomServiceRegister.cs (register services)
```

---

## 🔧 Implementation Steps

1. **Setup Anchor Project**
   - Install Anchor framework
   - Create new Anchor project
   - Write Solana program code
   - Build and test on devnet

2. **Deploy Program to Devnet**
   - Deploy program
   - Get program ID
   - Test initialization
   - Test updates

3. **Create C# Solana Client**
   - Install Solana NuGet packages
   - Create service interface
   - Implement PDA derivation
   - Implement transaction building

4. **Implement Publishing Service**
   - Initialize funding rate accounts
   - Build update transactions
   - Sign and send transactions
   - Handle transaction confirmation

5. **Implement Reading Service**
   - Read account data from PDA
   - Parse account data
   - Convert to C# models

6. **Add Error Handling**
   - Handle transaction failures
   - Retry logic for failed transactions
   - Logging and monitoring

7. **Create Scheduled Job**
   - Integrate with funding rate calculation service
   - Publish rates hourly
   - Handle failures gracefully

8. **Testing**
   - Unit tests for PDA derivation
   - Integration tests on devnet
   - Test with multiple symbols
   - Test transaction failures

9. **Deploy to Mainnet**
   - Deploy program to mainnet
   - Update configuration
   - Monitor for issues

---

## ✅ Acceptance Criteria

- [ ] Solana program compiles and deploys successfully
- [ ] PDA accounts can be initialized
- [ ] Funding rates can be updated on-chain
- [ ] Funding rates can be read from on-chain
- [ ] Transaction signing works correctly
- [ ] Transaction confirmation handled
- [ ] Batch publishing works (multiple symbols)
- [ ] Error handling for failed transactions
- [ ] Scheduled job publishes rates hourly
- [ ] Performance: <5 seconds per symbol publish
- [ ] Integration tests pass on devnet
- [ ] Program deployed to mainnet (or ready for deployment)

---

## 📊 Test Cases

### Test Case 1: Initialize Funding Rate Account

**Input:**
- Symbol: "AAPL"

**Expected:**
- PDA account created
- Account initialized with symbol
- Transaction confirmed

### Test Case 2: Update Funding Rate

**Input:**
- Symbol: "AAPL"
- Rate: 0.1% annualized
- Hourly Rate: 0.0000114% per hour
- Mark Price: $150.25
- Spot Price: $150.00

**Expected:**
- Account updated successfully
- Data readable from on-chain
- ValidUntil set correctly

### Test Case 3: Read Funding Rate

**Input:**
- Symbol: "AAPL"

**Expected:**
- Latest funding rate data retrieved
- All fields populated correctly
- Prices decoded correctly

### Test Case 4: Batch Update

**Input:**
- Symbols: ["AAPL", "MSFT", "GOOGL"]
- Rates for each

**Expected:**
- All accounts updated
- Transactions confirmed
- All data readable

### Test Case 5: Transaction Failure Handling

**Scenario:** Insufficient funds or network error

**Expected:**
- Error caught and logged
- Retry mechanism (if applicable)
- Alert sent (if critical)

---

## 🔗 Related Tasks

- **Task 19:** Funding Rate Calculation Service (depends on - provides rates to publish)
- **Task 23:** API Endpoints - Funding Rates (may expose on-chain data)
- **Task 26:** Frontend - Funding Rate Monitor (displays on-chain status)

---

## 📚 References

- [Solana Program Derived Addresses](https://docs.solana.com/developing/programming-model/calling-between-programs#program-derived-addresses)
- [Anchor Framework Documentation](https://www.anchor-lang.com/docs)
- [Solnet Documentation](https://github.com/bmresearch/Solnet)
- [Solana Transaction Format](https://docs.solana.com/developing/programming-model/transactions)

---

## 💡 Notes

- Start with devnet for all development and testing
- Use separate wallet for oracle (not main wallet)
- Consider using versioned transactions (v0) for better reliability
- Monitor transaction fees (can add up with hourly updates)
- Consider batching multiple updates in single transaction if possible
- Store program ID and PDA addresses in configuration
- Consider using Anchor's TypeScript client for easier integration (alternative to C#)

---

## ⚠️ Security Considerations

- Private key should be encrypted and stored securely
- Use separate wallet for oracle operations
- Implement rate limiting on publishing
- Validate all data before publishing
- Consider multi-sig for production (requires multiple signatures)

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
