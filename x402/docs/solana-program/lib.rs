/**
 * OASIS x402 Revenue Distribution
 * Solana Program (Smart Contract)
 * 
 * Handles automatic payment distribution to NFT holders
 */

use anchor_lang::prelude::*;
use anchor_spl::token::{self, Token, TokenAccount, Transfer};
use anchor_spl::associated_token::AssociatedToken;

declare_id!("OASIS402PaymentDistributor11111111111111111");

#[program]
pub mod oasis_x402_distributor {
    use super::*;

    /**
     * Initialize the distribution program
     */
    pub fn initialize(
        ctx: Context<Initialize>,
        bump: u8,
    ) -> Result<()> {
        let distributor = &mut ctx.accounts.distributor;
        distributor.authority = ctx.accounts.authority.key();
        distributor.bump = bump;
        distributor.total_distributions = 0;
        distributor.total_amount_distributed = 0;
        
        msg!("OASIS x402 Distributor initialized");
        Ok(())
    }

    /**
     * Register an NFT collection for x402 revenue distribution
     */
    pub fn register_nft_collection(
        ctx: Context<RegisterNFT>,
        nft_mint: Pubkey,
        x402_endpoint: String,
        revenue_model: RevenueModel,
    ) -> Result<()> {
        let nft_config = &mut ctx.accounts.nft_config;
        nft_config.nft_mint = nft_mint;
        nft_config.x402_endpoint = x402_endpoint;
        nft_config.revenue_model = revenue_model;
        nft_config.total_distributed = 0;
        nft_config.distribution_count = 0;
        nft_config.is_active = true;
        
        msg!("NFT collection registered for x402: {}", nft_mint);
        Ok(())
    }

    /**
     * Distribute payment to all NFT holders
     * Called by x402 webhook
     */
    pub fn distribute_payment(
        ctx: Context<DistributePayment>,
        amount: u64,
        holder_addresses: Vec<Pubkey>,
    ) -> Result<()> {
        require!(ctx.accounts.nft_config.is_active, ErrorCode::NFTNotActive);
        require!(holder_addresses.len() > 0, ErrorCode::NoHolders);
        
        let distributor = &mut ctx.accounts.distributor;
        let nft_config = &mut ctx.accounts.nft_config;
        
        // Calculate amount per holder (equal distribution)
        let holder_count = holder_addresses.len() as u64;
        let platform_fee = amount * 25 / 1000; // 2.5% fee
        let distributable_amount = amount - platform_fee;
        let amount_per_holder = distributable_amount / holder_count;
        
        msg!(
            "Distributing {} lamports to {} holders ({} each)",
            amount,
            holder_count,
            amount_per_holder
        );
        
        // Transfer from treasury to each holder
        // Note: In production, this would iterate through holder accounts
        // For this POC, we simulate the distribution
        
        // Transfer platform fee to fee account
        let cpi_accounts = Transfer {
            from: ctx.accounts.treasury_account.to_account_info(),
            to: ctx.accounts.fee_account.to_account_info(),
            authority: ctx.accounts.authority.to_account_info(),
        };
        let cpi_program = ctx.accounts.token_program.to_account_info();
        let cpi_ctx = CpiContext::new(cpi_program, cpi_accounts);
        token::transfer(cpi_ctx, platform_fee)?;
        
        // Update statistics
        distributor.total_distributions += 1;
        distributor.total_amount_distributed += amount;
        nft_config.total_distributed += amount;
        nft_config.distribution_count += 1;
        
        emit!(PaymentDistributed {
            nft_mint: nft_config.nft_mint,
            amount: amount,
            holder_count: holder_count,
            amount_per_holder: amount_per_holder,
            timestamp: Clock::get()?.unix_timestamp,
        });
        
        Ok(())
    }

    /**
     * Batch distribute to multiple holders (gas optimized)
     */
    pub fn distribute_batch(
        ctx: Context<DistributeBatch>,
        amounts: Vec<u64>,
    ) -> Result<()> {
        require!(
            ctx.remaining_accounts.len() == amounts.len(),
            ErrorCode::AccountMismatch
        );
        
        // Iterate through remaining accounts and transfer
        for (i, holder_account) in ctx.remaining_accounts.iter().enumerate() {
            let amount = amounts[i];
            
            // Transfer SPL token or SOL
            let cpi_accounts = Transfer {
                from: ctx.accounts.treasury_account.to_account_info(),
                to: holder_account.to_account_info(),
                authority: ctx.accounts.authority.to_account_info(),
            };
            let cpi_program = ctx.accounts.token_program.to_account_info();
            let cpi_ctx = CpiContext::new(cpi_program, cpi_accounts);
            token::transfer(cpi_ctx, amount)?;
            
            msg!("Transferred {} to holder {}", amount, holder_account.key());
        }
        
        msg!("Batch distribution complete: {} holders", amounts.len());
        Ok(())
    }

    /**
     * Query distribution stats for an NFT
     */
    pub fn get_stats(
        ctx: Context<GetStats>,
    ) -> Result<()> {
        let nft_config = &ctx.accounts.nft_config;
        
        msg!("NFT Stats: mint={}", nft_config.nft_mint);
        msg!("Total Distributed: {}", nft_config.total_distributed);
        msg!("Distribution Count: {}", nft_config.distribution_count);
        msg!("Active: {}", nft_config.is_active);
        
        Ok(())
    }
}

// ============================================================================
// Account Structures
// ============================================================================

#[derive(Accounts)]
pub struct Initialize<'info> {
    #[account(
        init,
        payer = authority,
        space = 8 + Distributor::LEN,
        seeds = [b"distributor"],
        bump
    )]
    pub distributor: Account<'info, Distributor>,
    
    #[account(mut)]
    pub authority: Signer<'info>,
    
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct RegisterNFT<'info> {
    #[account(
        init,
        payer = authority,
        space = 8 + NFTConfig::LEN,
        seeds = [b"nft_config", nft_mint.key().as_ref()],
        bump
    )]
    pub nft_config: Account<'info, NFTConfig>,
    
    /// CHECK: This is the NFT mint address being registered
    pub nft_mint: AccountInfo<'info>,
    
    #[account(mut)]
    pub authority: Signer<'info>,
    
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct DistributePayment<'info> {
    #[account(
        mut,
        seeds = [b"distributor"],
        bump = distributor.bump
    )]
    pub distributor: Account<'info, Distributor>,
    
    #[account(
        mut,
        seeds = [b"nft_config", nft_config.nft_mint.as_ref()],
        bump
    )]
    pub nft_config: Account<'info, NFTConfig>,
    
    #[account(mut)]
    pub treasury_account: Account<'info, TokenAccount>,
    
    #[account(mut)]
    pub fee_account: Account<'info, TokenAccount>,
    
    pub authority: Signer<'info>,
    
    pub token_program: Program<'info, Token>,
}

#[derive(Accounts)]
pub struct DistributeBatch<'info> {
    #[account(mut)]
    pub treasury_account: Account<'info, TokenAccount>,
    
    pub authority: Signer<'info>,
    
    pub token_program: Program<'info, Token>,
    
    // Remaining accounts: holder token accounts
}

#[derive(Accounts)]
pub struct GetStats<'info> {
    #[account(
        seeds = [b"nft_config", nft_config.nft_mint.as_ref()],
        bump
    )]
    pub nft_config: Account<'info, NFTConfig>,
}

// ============================================================================
// Data Structures
// ============================================================================

#[account]
pub struct Distributor {
    pub authority: Pubkey,
    pub bump: u8,
    pub total_distributions: u64,
    pub total_amount_distributed: u64,
}

impl Distributor {
    pub const LEN: usize = 32 + 1 + 8 + 8;
}

#[account]
pub struct NFTConfig {
    pub nft_mint: Pubkey,
    pub x402_endpoint: String,
    pub revenue_model: RevenueModel,
    pub total_distributed: u64,
    pub distribution_count: u64,
    pub is_active: bool,
}

impl NFTConfig {
    pub const LEN: usize = 32 + 256 + 1 + 8 + 8 + 1;
}

#[derive(AnchorSerialize, AnchorDeserialize, Clone, PartialEq, Eq)]
pub enum RevenueModel {
    Equal,          // Equal split among all holders
    Weighted,       // Weighted by token holdings
    CreatorSplit,   // Fixed % to creator, rest to holders
}

// ============================================================================
// Events
// ============================================================================

#[event]
pub struct PaymentDistributed {
    pub nft_mint: Pubkey,
    pub amount: u64,
    pub holder_count: u64,
    pub amount_per_holder: u64,
    pub timestamp: i64,
}

// ============================================================================
// Errors
// ============================================================================

#[error_code]
pub enum ErrorCode {
    #[msg("NFT collection is not active")]
    NFTNotActive,
    
    #[msg("No holders found for this NFT")]
    NoHolders,
    
    #[msg("Account count mismatch")]
    AccountMismatch,
    
    #[msg("Insufficient funds for distribution")]
    InsufficientFunds,
    
    #[msg("Unauthorized")]
    Unauthorized,
}

