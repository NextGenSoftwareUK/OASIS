use anchor_lang::prelude::*;
use anchor_spl::token::{self, Token, TokenAccount, Mint, Transfer};

declare_id!("DAtYXMpDEZL8RqQ7KVZp9YvKj8TqPSxKjHD2nNKFZC3L");

/// DAT Integration: Programmable Digital Asset Treasury
/// Combines SOL staking with tokenized asset yields (Music, Property, Sports, Wine, Film)
/// Core innovation: Transform plain vanilla DATs into dynamic, yield-generating vehicles
#[program]
pub mod dat_integration {
    use super::*;

    /// Initialize a new Digital Asset Treasury
    /// Sets up the treasury with governance and staking parameters
    pub fn initialize_treasury(
        ctx: Context<InitializeTreasury>,
        treasury_name: String,
        sol_staking_apy: u16,        // Basis points (500 = 5%)
        minimum_stake: u64,
        lockup_period: i64,           // Seconds
    ) -> Result<()> {
        let treasury = &mut ctx.accounts.treasury;
        
        treasury.authority = ctx.accounts.authority.key();
        treasury.name = treasury_name;
        treasury.sol_staking_apy = sol_staking_apy;
        treasury.minimum_stake = minimum_stake;
        treasury.lockup_period = lockup_period;
        treasury.total_sol_staked = 0;
        treasury.total_assets_value = 0;
        treasury.total_yield_distributed = 0;
        treasury.is_active = true;
        treasury.created_at = Clock::get()?.unix_timestamp;
        treasury.bump = ctx.bumps.treasury;

        emit!(TreasuryInitialized {
            treasury: treasury.key(),
            authority: treasury.authority,
            name: treasury.name.clone(),
            timestamp: treasury.created_at,
        });

        Ok(())
    }

    /// Add a tokenized asset to the treasury
    /// Supports: Music IP, Real Estate, Sports, Wine, Film
    pub fn add_asset(
        ctx: Context<AddAsset>,
        asset_type: AssetType,
        asset_name: String,
        asset_value: u64,
        annual_yield_bps: u16,        // Expected annual yield in basis points
        metadata_uri: String,
    ) -> Result<()> {
        let treasury = &mut ctx.accounts.treasury;
        let asset = &mut ctx.accounts.asset;

        require!(treasury.is_active, ErrorCode::TreasuryInactive);

        asset.treasury = treasury.key();
        asset.asset_type = asset_type;
        asset.name = asset_name;
        asset.value = asset_value;
        asset.annual_yield_bps = annual_yield_bps;
        asset.metadata_uri = metadata_uri;
        asset.total_distributions = 0;
        asset.is_active = true;
        asset.added_at = Clock::get()?.unix_timestamp;
        asset.bump = ctx.bumps.asset;

        // Update treasury total assets value
        treasury.total_assets_value = treasury.total_assets_value
            .checked_add(asset_value)
            .ok_or(ErrorCode::MathOverflow)?;

        emit!(AssetAdded {
            treasury: treasury.key(),
            asset: asset.key(),
            asset_type: asset.asset_type,
            value: asset.value,
            yield_bps: asset.annual_yield_bps,
        });

        Ok(())
    }

    /// Stake SOL into the treasury
    /// Users stake SOL and receive enhanced yield from SOL staking + asset returns
    pub fn stake_sol(
        ctx: Context<StakeSol>,
        amount: u64,
    ) -> Result<()> {
        let treasury = &mut ctx.accounts.treasury;
        let stake_account = &mut ctx.accounts.stake_account;

        require!(treasury.is_active, ErrorCode::TreasuryInactive);
        require!(amount >= treasury.minimum_stake, ErrorCode::BelowMinimumStake);

        // Transfer SOL to treasury vault
        let transfer_instruction = anchor_lang::solana_program::system_instruction::transfer(
            &ctx.accounts.user.key(),
            &ctx.accounts.treasury_vault.key(),
            amount,
        );

        anchor_lang::solana_program::program::invoke(
            &transfer_instruction,
            &[
                ctx.accounts.user.to_account_info(),
                ctx.accounts.treasury_vault.to_account_info(),
                ctx.accounts.system_program.to_account_info(),
            ],
        )?;

        // Initialize or update stake account
        if stake_account.amount == 0 {
            stake_account.user = ctx.accounts.user.key();
            stake_account.treasury = treasury.key();
            stake_account.amount = amount;
            stake_account.staked_at = Clock::get()?.unix_timestamp;
            stake_account.unlock_time = stake_account.staked_at + treasury.lockup_period;
            stake_account.rewards_claimed = 0;
            stake_account.bump = ctx.bumps.stake_account;
        } else {
            stake_account.amount = stake_account.amount
                .checked_add(amount)
                .ok_or(ErrorCode::MathOverflow)?;
        }

        // Update treasury total staked
        treasury.total_sol_staked = treasury.total_sol_staked
            .checked_add(amount)
            .ok_or(ErrorCode::MathOverflow)?;

        emit!(SolStaked {
            user: ctx.accounts.user.key(),
            treasury: treasury.key(),
            amount,
            total_staked: stake_account.amount,
        });

        Ok(())
    }

    /// Calculate and claim enhanced yield
    /// Yield = SOL Staking APY + Weighted Asset Yields
    pub fn claim_yield(ctx: Context<ClaimYield>) -> Result<()> {
        let treasury = &mut ctx.accounts.treasury;
        let stake_account = &mut ctx.accounts.stake_account;

        require!(treasury.is_active, ErrorCode::TreasuryInactive);
        require!(stake_account.amount > 0, ErrorCode::NoStakeFound);

        let current_time = Clock::get()?.unix_timestamp;
        let time_staked = current_time - stake_account.staked_at;

        // Calculate SOL staking yield (simple interest for demo)
        let sol_yield = (stake_account.amount as u128)
            .checked_mul(treasury.sol_staking_apy as u128)
            .and_then(|v| v.checked_mul(time_staked as u128))
            .and_then(|v| v.checked_div(10000))  // basis points
            .and_then(|v| v.checked_div(31536000)) // seconds per year
            .ok_or(ErrorCode::MathOverflow)? as u64;

        // Calculate proportional share of asset yields
        let user_share_bps = if treasury.total_sol_staked > 0 {
            ((stake_account.amount as u128)
                .checked_mul(10000)
                .and_then(|v| v.checked_div(treasury.total_sol_staked as u128))
                .ok_or(ErrorCode::MathOverflow)? as u64)
        } else {
            0
        };

        // For simplicity, assume average asset yield (in production, iterate through assets)
        let asset_yield = if treasury.total_assets_value > 0 {
            (treasury.total_assets_value as u128)
                .checked_mul(user_share_bps as u128)
                .and_then(|v| v.checked_mul(500)) // Assume 5% average asset yield
                .and_then(|v| v.checked_mul(time_staked as u128))
                .and_then(|v| v.checked_div(100000000)) // basis points normalization
                .and_then(|v| v.checked_div(31536000))
                .ok_or(ErrorCode::MathOverflow)? as u64
        } else {
            0
        };

        let total_yield = sol_yield
            .checked_add(asset_yield)
            .ok_or(ErrorCode::MathOverflow)?;

        require!(total_yield > 0, ErrorCode::NoYieldAvailable);

        // Transfer yield from treasury vault to user
        **ctx.accounts.treasury_vault.to_account_info().try_borrow_mut_lamports()? -= total_yield;
        **ctx.accounts.user.try_borrow_mut_lamports()? += total_yield;

        // Update accounting
        stake_account.rewards_claimed = stake_account.rewards_claimed
            .checked_add(total_yield)
            .ok_or(ErrorCode::MathOverflow)?;
        
        stake_account.staked_at = current_time; // Reset for next calculation

        treasury.total_yield_distributed = treasury.total_yield_distributed
            .checked_add(total_yield)
            .ok_or(ErrorCode::MathOverflow)?;

        emit!(YieldClaimed {
            user: ctx.accounts.user.key(),
            sol_yield,
            asset_yield,
            total_yield,
        });

        Ok(())
    }

    /// Unstake SOL after lockup period
    pub fn unstake_sol(ctx: Context<UnstakeSol>, amount: u64) -> Result<()> {
        let treasury = &mut ctx.accounts.treasury;
        let stake_account = &mut ctx.accounts.stake_account;

        require!(stake_account.amount >= amount, ErrorCode::InsufficientStake);
        
        let current_time = Clock::get()?.unix_timestamp;
        require!(
            current_time >= stake_account.unlock_time,
            ErrorCode::StillLocked
        );

        // Transfer SOL back to user
        **ctx.accounts.treasury_vault.to_account_info().try_borrow_mut_lamports()? -= amount;
        **ctx.accounts.user.try_borrow_mut_lamports()? += amount;

        // Update accounting
        stake_account.amount = stake_account.amount
            .checked_sub(amount)
            .ok_or(ErrorCode::MathOverflow)?;

        treasury.total_sol_staked = treasury.total_sol_staked
            .checked_sub(amount)
            .ok_or(ErrorCode::MathOverflow)?;

        emit!(SolUnstaked {
            user: ctx.accounts.user.key(),
            amount,
            remaining: stake_account.amount,
        });

        Ok(())
    }

    /// Distribute asset-specific returns (called by asset managers)
    pub fn distribute_asset_yield(
        ctx: Context<DistributeAssetYield>,
        amount: u64,
    ) -> Result<()> {
        let treasury = &mut ctx.accounts.treasury;
        let asset = &mut ctx.accounts.asset;

        require!(treasury.is_active, ErrorCode::TreasuryInactive);
        require!(asset.is_active, ErrorCode::AssetInactive);

        // Transfer yield to treasury vault
        let transfer_instruction = anchor_lang::solana_program::system_instruction::transfer(
            &ctx.accounts.asset_manager.key(),
            &ctx.accounts.treasury_vault.key(),
            amount,
        );

        anchor_lang::solana_program::program::invoke(
            &transfer_instruction,
            &[
                ctx.accounts.asset_manager.to_account_info(),
                ctx.accounts.treasury_vault.to_account_info(),
                ctx.accounts.system_program.to_account_info(),
            ],
        )?;

        asset.total_distributions = asset.total_distributions
            .checked_add(amount)
            .ok_or(ErrorCode::MathOverflow)?;

        emit!(AssetYieldDistributed {
            asset: asset.key(),
            amount,
            total_distributions: asset.total_distributions,
        });

        Ok(())
    }

    /// Get enhanced APY (SOL staking + weighted asset yields)
    pub fn get_total_apy(ctx: Context<GetTotalApy>) -> Result<u16> {
        let treasury = &ctx.accounts.treasury;
        
        // Base SOL staking APY
        let mut total_apy = treasury.sol_staking_apy;

        // In production, calculate weighted asset yields based on portfolio
        // For now, add estimated asset yield boost (e.g., 10-15% from assets)
        let asset_boost = 1000; // 10% in basis points

        total_apy = total_apy
            .checked_add(asset_boost)
            .ok_or(ErrorCode::MathOverflow)?;

        msg!("Total Enhanced APY: {}%", total_apy as f64 / 100.0);

        Ok(total_apy)
    }
}

// ============================================================================
// Account Structures
// ============================================================================

#[account]
pub struct Treasury {
    pub authority: Pubkey,
    pub name: String,
    pub sol_staking_apy: u16,
    pub minimum_stake: u64,
    pub lockup_period: i64,
    pub total_sol_staked: u64,
    pub total_assets_value: u64,
    pub total_yield_distributed: u64,
    pub is_active: bool,
    pub created_at: i64,
    pub bump: u8,
}

#[account]
pub struct Asset {
    pub treasury: Pubkey,
    pub asset_type: AssetType,
    pub name: String,
    pub value: u64,
    pub annual_yield_bps: u16,
    pub metadata_uri: String,
    pub total_distributions: u64,
    pub is_active: bool,
    pub added_at: i64,
    pub bump: u8,
}

#[account]
pub struct StakeAccount {
    pub user: Pubkey,
    pub treasury: Pubkey,
    pub amount: u64,
    pub staked_at: i64,
    pub unlock_time: i64,
    pub rewards_claimed: u64,
    pub bump: u8,
}

// ============================================================================
// Context Structures
// ============================================================================

#[derive(Accounts)]
#[instruction(treasury_name: String)]
pub struct InitializeTreasury<'info> {
    #[account(
        init,
        payer = authority,
        space = 8 + 32 + 256 + 2 + 8 + 8 + 8 + 8 + 8 + 1 + 8 + 1,
        seeds = [b"treasury", authority.key().as_ref(), treasury_name.as_bytes()],
        bump
    )]
    pub treasury: Account<'info, Treasury>,
    
    /// CHECK: Treasury vault to hold staked SOL
    #[account(mut)]
    pub treasury_vault: AccountInfo<'info>,
    
    #[account(mut)]
    pub authority: Signer<'info>,
    
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
#[instruction(asset_name: String)]
pub struct AddAsset<'info> {
    #[account(
        mut,
        has_one = authority,
    )]
    pub treasury: Account<'info, Treasury>,
    
    #[account(
        init,
        payer = authority,
        space = 8 + 32 + 1 + 256 + 8 + 2 + 512 + 8 + 1 + 8 + 1,
        seeds = [b"asset", treasury.key().as_ref(), asset_name.as_bytes()],
        bump
    )]
    pub asset: Account<'info, Asset>,
    
    #[account(mut)]
    pub authority: Signer<'info>,
    
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct StakeSol<'info> {
    #[account(mut)]
    pub treasury: Account<'info, Treasury>,
    
    #[account(
        init_if_needed,
        payer = user,
        space = 8 + 32 + 32 + 8 + 8 + 8 + 8 + 1,
        seeds = [b"stake", treasury.key().as_ref(), user.key().as_ref()],
        bump
    )]
    pub stake_account: Account<'info, StakeAccount>,
    
    /// CHECK: Treasury vault to receive SOL
    #[account(mut)]
    pub treasury_vault: AccountInfo<'info>,
    
    #[account(mut)]
    pub user: Signer<'info>,
    
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct ClaimYield<'info> {
    #[account(mut)]
    pub treasury: Account<'info, Treasury>,
    
    #[account(
        mut,
        seeds = [b"stake", treasury.key().as_ref(), user.key().as_ref()],
        bump = stake_account.bump,
    )]
    pub stake_account: Account<'info, StakeAccount>,
    
    /// CHECK: Treasury vault to send yield from
    #[account(mut)]
    pub treasury_vault: AccountInfo<'info>,
    
    #[account(mut)]
    pub user: Signer<'info>,
}

#[derive(Accounts)]
pub struct UnstakeSol<'info> {
    #[account(mut)]
    pub treasury: Account<'info, Treasury>,
    
    #[account(
        mut,
        seeds = [b"stake", treasury.key().as_ref(), user.key().as_ref()],
        bump = stake_account.bump,
    )]
    pub stake_account: Account<'info, StakeAccount>,
    
    /// CHECK: Treasury vault to send SOL from
    #[account(mut)]
    pub treasury_vault: AccountInfo<'info>,
    
    #[account(mut)]
    pub user: Signer<'info>,
}

#[derive(Accounts)]
pub struct DistributeAssetYield<'info> {
    #[account(mut)]
    pub treasury: Account<'info, Treasury>,
    
    #[account(
        mut,
        has_one = treasury,
    )]
    pub asset: Account<'info, Asset>,
    
    /// CHECK: Treasury vault to receive yield
    #[account(mut)]
    pub treasury_vault: AccountInfo<'info>,
    
    #[account(mut)]
    pub asset_manager: Signer<'info>,
    
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct GetTotalApy<'info> {
    pub treasury: Account<'info, Treasury>,
}

// ============================================================================
// Enums and Types
// ============================================================================

#[derive(AnchorSerialize, AnchorDeserialize, Clone, Copy, PartialEq, Eq)]
pub enum AssetType {
    MusicIP,
    RealEstate,
    Sports,
    Wine,
    Film,
}

// ============================================================================
// Events
// ============================================================================

#[event]
pub struct TreasuryInitialized {
    pub treasury: Pubkey,
    pub authority: Pubkey,
    pub name: String,
    pub timestamp: i64,
}

#[event]
pub struct AssetAdded {
    pub treasury: Pubkey,
    pub asset: Pubkey,
    pub asset_type: AssetType,
    pub value: u64,
    pub yield_bps: u16,
}

#[event]
pub struct SolStaked {
    pub user: Pubkey,
    pub treasury: Pubkey,
    pub amount: u64,
    pub total_staked: u64,
}

#[event]
pub struct YieldClaimed {
    pub user: Pubkey,
    pub sol_yield: u64,
    pub asset_yield: u64,
    pub total_yield: u64,
}

#[event]
pub struct SolUnstaked {
    pub user: Pubkey,
    pub amount: u64,
    pub remaining: u64,
}

#[event]
pub struct AssetYieldDistributed {
    pub asset: Pubkey,
    pub amount: u64,
    pub total_distributions: u64,
}

// ============================================================================
// Errors
// ============================================================================

#[error_code]
pub enum ErrorCode {
    #[msg("Treasury is not active")]
    TreasuryInactive,
    #[msg("Asset is not active")]
    AssetInactive,
    #[msg("Amount below minimum stake requirement")]
    BelowMinimumStake,
    #[msg("No stake found for this user")]
    NoStakeFound,
    #[msg("Stake is still locked")]
    StillLocked,
    #[msg("Insufficient stake amount")]
    InsufficientStake,
    #[msg("No yield available to claim")]
    NoYieldAvailable,
    #[msg("Math overflow")]
    MathOverflow,
}






