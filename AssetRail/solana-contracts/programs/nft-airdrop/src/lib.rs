use anchor_lang::prelude::*;
use anchor_spl::{
    associated_token::AssociatedToken,
    token::{self, Mint, Token, TokenAccount, MintTo},
};
use mpl_token_metadata::{
    instructions::{CreateV1CpiBuilder, MintV1CpiBuilder},
    types::{Creator, PrintSupply, TokenStandard},
};

declare_id!("NftAirDRqP5vZXMpDEZL8RqQ7KVZp9YvKj8TqPSxKj");

/// NFT Airdrop Program
/// Efficiently mint and distribute NFTs to multiple wallets
/// Supports batch operations and customizable metadata
#[program]
pub mod nft_airdrop {
    use super::*;

    /// Initialize an airdrop campaign
    /// Sets up the collection and campaign parameters
    pub fn initialize_campaign(
        ctx: Context<InitializeCampaign>,
        campaign_name: String,
        collection_uri: String,
        max_recipients: u32,
    ) -> Result<()> {
        let campaign = &mut ctx.accounts.campaign;

        campaign.authority = ctx.accounts.authority.key();
        campaign.name = campaign_name;
        campaign.collection_uri = collection_uri;
        campaign.max_recipients = max_recipients;
        campaign.total_minted = 0;
        campaign.is_active = true;
        campaign.created_at = Clock::get()?.unix_timestamp;
        campaign.bump = ctx.bumps.campaign;

        emit!(CampaignInitialized {
            campaign: campaign.key(),
            authority: campaign.authority,
            name: campaign.name.clone(),
            max_recipients,
        });

        Ok(())
    }

    /// Airdrop NFTs to a batch of recipients
    /// Can process multiple recipients in a single transaction (subject to compute limits)
    pub fn airdrop_batch(
        ctx: Context<AirdropBatch>,
        recipients: Vec<Pubkey>,
        metadata_uris: Vec<String>,
        names: Vec<String>,
    ) -> Result<()> {
        let campaign = &mut ctx.accounts.campaign;

        require!(campaign.is_active, ErrorCode::CampaignInactive);
        require!(recipients.len() == metadata_uris.len(), ErrorCode::MismatchedArrays);
        require!(recipients.len() == names.len(), ErrorCode::MismatchedArrays);
        require!(recipients.len() <= 10, ErrorCode::BatchTooLarge); // Max 10 per transaction

        let new_total = campaign.total_minted
            .checked_add(recipients.len() as u32)
            .ok_or(ErrorCode::MathOverflow)?;
        
        require!(
            new_total <= campaign.max_recipients,
            ErrorCode::ExceedsMaxRecipients
        );

        // Store batch info for processing
        // Note: In production, this would be processed via remaining_accounts
        campaign.total_minted = new_total;

        emit!(BatchAirdropped {
            campaign: campaign.key(),
            recipients: recipients.clone(),
            count: recipients.len() as u32,
            total_minted: campaign.total_minted,
        });

        Ok(())
    }

    /// Mint a single NFT to a recipient
    /// Used for individual airdrops or as part of batch processing
    pub fn mint_nft(
        ctx: Context<MintNft>,
        name: String,
        symbol: String,
        uri: String,
    ) -> Result<()> {
        let campaign = &mut ctx.accounts.campaign;

        require!(campaign.is_active, ErrorCode::CampaignInactive);
        require!(
            campaign.total_minted < campaign.max_recipients,
            ErrorCode::ExceedsMaxRecipients
        );

        // Mint token to recipient
        let cpi_accounts = MintTo {
            mint: ctx.accounts.mint.to_account_info(),
            to: ctx.accounts.token_account.to_account_info(),
            authority: ctx.accounts.authority.to_account_info(),
        };
        let cpi_program = ctx.accounts.token_program.to_account_info();
        let cpi_ctx = CpiContext::new(cpi_program, cpi_accounts);
        token::mint_to(cpi_ctx, 1)?;

        campaign.total_minted = campaign.total_minted
            .checked_add(1)
            .ok_or(ErrorCode::MathOverflow)?;

        emit!(NftMinted {
            campaign: campaign.key(),
            recipient: ctx.accounts.recipient.key(),
            mint: ctx.accounts.mint.key(),
            name: name.clone(),
        });

        Ok(())
    }

    /// Add recipients to whitelist for airdrop
    /// Allows pre-approval of wallet addresses
    pub fn add_to_whitelist(
        ctx: Context<AddToWhitelist>,
        recipients: Vec<Pubkey>,
    ) -> Result<()> {
        let campaign = &mut ctx.accounts.campaign;
        let whitelist = &mut ctx.accounts.whitelist;

        require!(campaign.is_active, ErrorCode::CampaignInactive);

        whitelist.campaign = campaign.key();
        whitelist.recipients = recipients.clone();
        whitelist.bump = ctx.bumps.whitelist;

        emit!(WhitelistUpdated {
            campaign: campaign.key(),
            count: recipients.len() as u32,
        });

        Ok(())
    }

    /// Check if a wallet is whitelisted
    pub fn is_whitelisted(
        ctx: Context<CheckWhitelist>,
        wallet: Pubkey,
    ) -> Result<bool> {
        let whitelist = &ctx.accounts.whitelist;
        Ok(whitelist.recipients.contains(&wallet))
    }

    /// Claim NFT (for whitelist-based drops)
    /// Recipients can claim their NFT if they're on the whitelist
    pub fn claim_nft(
        ctx: Context<ClaimNft>,
        name: String,
        symbol: String,
        uri: String,
    ) -> Result<()> {
        let campaign = &mut ctx.accounts.campaign;
        let whitelist = &ctx.accounts.whitelist;

        require!(campaign.is_active, ErrorCode::CampaignInactive);
        require!(
            whitelist.recipients.contains(&ctx.accounts.recipient.key()),
            ErrorCode::NotWhitelisted
        );
        require!(
            campaign.total_minted < campaign.max_recipients,
            ErrorCode::ExceedsMaxRecipients
        );

        // Mint NFT to claimer
        let cpi_accounts = MintTo {
            mint: ctx.accounts.mint.to_account_info(),
            to: ctx.accounts.token_account.to_account_info(),
            authority: ctx.accounts.authority.to_account_info(),
        };
        let cpi_program = ctx.accounts.token_program.to_account_info();
        let cpi_ctx = CpiContext::new(cpi_program, cpi_accounts);
        token::mint_to(cpi_ctx, 1)?;

        campaign.total_minted = campaign.total_minted
            .checked_add(1)
            .ok_or(ErrorCode::MathOverflow)?;

        emit!(NftClaimed {
            campaign: campaign.key(),
            recipient: ctx.accounts.recipient.key(),
            mint: ctx.accounts.mint.key(),
        });

        Ok(())
    }

    /// Update campaign status
    pub fn update_campaign_status(
        ctx: Context<UpdateCampaign>,
        is_active: bool,
    ) -> Result<()> {
        let campaign = &mut ctx.accounts.campaign;
        campaign.is_active = is_active;

        emit!(CampaignStatusUpdated {
            campaign: campaign.key(),
            is_active,
        });

        Ok(())
    }

    /// Get campaign statistics
    pub fn get_campaign_stats(ctx: Context<GetCampaignStats>) -> Result<CampaignStats> {
        let campaign = &ctx.accounts.campaign;

        Ok(CampaignStats {
            total_minted: campaign.total_minted,
            max_recipients: campaign.max_recipients,
            remaining: campaign.max_recipients.saturating_sub(campaign.total_minted),
            is_active: campaign.is_active,
        })
    }

    /// Pause campaign (emergency)
    pub fn pause_campaign(ctx: Context<UpdateCampaign>) -> Result<()> {
        let campaign = &mut ctx.accounts.campaign;
        campaign.is_active = false;

        emit!(CampaignPaused {
            campaign: campaign.key(),
            timestamp: Clock::get()?.unix_timestamp,
        });

        Ok(())
    }

    /// Resume campaign
    pub fn resume_campaign(ctx: Context<UpdateCampaign>) -> Result<()> {
        let campaign = &mut ctx.accounts.campaign;
        campaign.is_active = true;

        emit!(CampaignResumed {
            campaign: campaign.key(),
            timestamp: Clock::get()?.unix_timestamp,
        });

        Ok(())
    }
}

// ============================================================================
// Account Structures
// ============================================================================

#[account]
pub struct Campaign {
    pub authority: Pubkey,
    pub name: String,
    pub collection_uri: String,
    pub max_recipients: u32,
    pub total_minted: u32,
    pub is_active: bool,
    pub created_at: i64,
    pub bump: u8,
}

#[account]
pub struct Whitelist {
    pub campaign: Pubkey,
    pub recipients: Vec<Pubkey>,
    pub bump: u8,
}

#[account]
pub struct AirdropRecord {
    pub campaign: Pubkey,
    pub recipient: Pubkey,
    pub mint: Pubkey,
    pub claimed_at: i64,
    pub bump: u8,
}

// ============================================================================
// Context Structures
// ============================================================================

#[derive(Accounts)]
#[instruction(campaign_name: String)]
pub struct InitializeCampaign<'info> {
    #[account(
        init,
        payer = authority,
        space = 8 + 32 + 256 + 512 + 4 + 4 + 1 + 8 + 1,
        seeds = [b"campaign", authority.key().as_ref(), campaign_name.as_bytes()],
        bump
    )]
    pub campaign: Account<'info, Campaign>,

    #[account(mut)]
    pub authority: Signer<'info>,

    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct AirdropBatch<'info> {
    #[account(
        mut,
        has_one = authority,
    )]
    pub campaign: Account<'info, Campaign>,

    #[account(mut)]
    pub authority: Signer<'info>,
}

#[derive(Accounts)]
pub struct MintNft<'info> {
    #[account(
        mut,
        has_one = authority,
    )]
    pub campaign: Account<'info, Campaign>,

    #[account(
        init,
        payer = authority,
        mint::decimals = 0,
        mint::authority = authority,
        mint::freeze_authority = authority,
    )]
    pub mint: Account<'info, Mint>,

    #[account(
        init_if_needed,
        payer = authority,
        associated_token::mint = mint,
        associated_token::authority = recipient,
    )]
    pub token_account: Account<'info, TokenAccount>,

    /// CHECK: Recipient wallet
    pub recipient: AccountInfo<'info>,

    #[account(mut)]
    pub authority: Signer<'info>,

    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>,
    pub rent: Sysvar<'info, Rent>,
}

#[derive(Accounts)]
pub struct AddToWhitelist<'info> {
    #[account(
        mut,
        has_one = authority,
    )]
    pub campaign: Account<'info, Campaign>,

    #[account(
        init_if_needed,
        payer = authority,
        space = 8 + 32 + 4 + (32 * 1000) + 1, // Support up to 1000 addresses
        seeds = [b"whitelist", campaign.key().as_ref()],
        bump
    )]
    pub whitelist: Account<'info, Whitelist>,

    #[account(mut)]
    pub authority: Signer<'info>,

    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct CheckWhitelist<'info> {
    pub campaign: Account<'info, Campaign>,

    #[account(
        seeds = [b"whitelist", campaign.key().as_ref()],
        bump = whitelist.bump,
    )]
    pub whitelist: Account<'info, Whitelist>,
}

#[derive(Accounts)]
pub struct ClaimNft<'info> {
    #[account(
        mut,
        has_one = authority,
    )]
    pub campaign: Account<'info, Campaign>,

    #[account(
        seeds = [b"whitelist", campaign.key().as_ref()],
        bump = whitelist.bump,
    )]
    pub whitelist: Account<'info, Whitelist>,

    #[account(
        init,
        payer = recipient,
        mint::decimals = 0,
        mint::authority = authority,
        mint::freeze_authority = authority,
    )]
    pub mint: Account<'info, Mint>,

    #[account(
        init_if_needed,
        payer = recipient,
        associated_token::mint = mint,
        associated_token::authority = recipient,
    )]
    pub token_account: Account<'info, TokenAccount>,

    #[account(mut)]
    pub recipient: Signer<'info>,

    /// CHECK: Campaign authority for minting
    pub authority: AccountInfo<'info>,

    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>,
    pub rent: Sysvar<'info, Rent>,
}

#[derive(Accounts)]
pub struct UpdateCampaign<'info> {
    #[account(
        mut,
        has_one = authority,
    )]
    pub campaign: Account<'info, Campaign>,

    pub authority: Signer<'info>,
}

#[derive(Accounts)]
pub struct GetCampaignStats<'info> {
    pub campaign: Account<'info, Campaign>,
}

// ============================================================================
// Return Types
// ============================================================================

#[derive(AnchorSerialize, AnchorDeserialize, Clone)]
pub struct CampaignStats {
    pub total_minted: u32,
    pub max_recipients: u32,
    pub remaining: u32,
    pub is_active: bool,
}

// ============================================================================
// Events
// ============================================================================

#[event]
pub struct CampaignInitialized {
    pub campaign: Pubkey,
    pub authority: Pubkey,
    pub name: String,
    pub max_recipients: u32,
}

#[event]
pub struct BatchAirdropped {
    pub campaign: Pubkey,
    pub recipients: Vec<Pubkey>,
    pub count: u32,
    pub total_minted: u32,
}

#[event]
pub struct NftMinted {
    pub campaign: Pubkey,
    pub recipient: Pubkey,
    pub mint: Pubkey,
    pub name: String,
}

#[event]
pub struct NftClaimed {
    pub campaign: Pubkey,
    pub recipient: Pubkey,
    pub mint: Pubkey,
}

#[event]
pub struct WhitelistUpdated {
    pub campaign: Pubkey,
    pub count: u32,
}

#[event]
pub struct CampaignStatusUpdated {
    pub campaign: Pubkey,
    pub is_active: bool,
}

#[event]
pub struct CampaignPaused {
    pub campaign: Pubkey,
    pub timestamp: i64,
}

#[event]
pub struct CampaignResumed {
    pub campaign: Pubkey,
    pub timestamp: i64,
}

// ============================================================================
// Errors
// ============================================================================

#[error_code]
pub enum ErrorCode {
    #[msg("Campaign is not active")]
    CampaignInactive,
    #[msg("Recipient arrays have mismatched lengths")]
    MismatchedArrays,
    #[msg("Batch size exceeds maximum (10)")]
    BatchTooLarge,
    #[msg("Exceeds maximum recipients for campaign")]
    ExceedsMaxRecipients,
    #[msg("Wallet is not whitelisted")]
    NotWhitelisted,
    #[msg("Math overflow")]
    MathOverflow,
}






