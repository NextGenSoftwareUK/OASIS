use anchor_lang::prelude::*;

declare_id!("F9TXnNiLChuEfZbRzvEsA6nQBsgC2bBF4VQEfEHhC9mh");

#[program]
pub mod rwa_oracle {
    use super::*;

    /// Initialize a funding rate account for a symbol
    pub fn initialize_funding_rate(
        ctx: Context<InitializeFundingRate>,
        symbol: String,
    ) -> Result<()> {
        let funding_rate = &mut ctx.accounts.funding_rate;
        funding_rate.symbol = symbol;
        funding_rate.authority = ctx.accounts.authority.key();
        funding_rate.bump = ctx.bumps.funding_rate;
        funding_rate.rate = 0;
        funding_rate.hourly_rate = 0;
        funding_rate.mark_price = 0;
        funding_rate.spot_price = 0;
        funding_rate.premium = 0;
        funding_rate.last_updated = Clock::get()?.unix_timestamp;
        funding_rate.valid_until = Clock::get()?.unix_timestamp;
        
        msg!("Initialized funding rate account for symbol: {}", funding_rate.symbol);
        Ok(())
    }

    /// Update funding rate for a symbol
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

        msg!("Updated funding rate for {}: rate={}, hourly_rate={}, mark={}, spot={}", 
            funding_rate.symbol, rate, hourly_rate, mark_price, spot_price);
        
        Ok(())
    }
}

#[account]
pub struct FundingRate {
    pub symbol: String, // Max 10 chars
    pub authority: Pubkey,
    pub rate: i64, // Annualized rate in basis points (10000 = 100%)
    pub hourly_rate: i64, // Hourly rate in basis points
    pub mark_price: u64,
    pub spot_price: u64,
    pub premium: i64,
    pub last_updated: i64, // Unix timestamp
    pub valid_until: i64, // Unix timestamp
    pub bump: u8,
}

impl FundingRate {
    pub const MAX_SIZE: usize = 4 + 10 + // discriminator + symbol (String with 10 chars max)
        32 + // authority (Pubkey)
        8 + // rate (i64)
        8 + // hourly_rate (i64)
        8 + // mark_price (u64)
        8 + // spot_price (u64)
        8 + // premium (i64)
        8 + // last_updated (i64)
        8 + // valid_until (i64)
        1; // bump (u8)
}

#[derive(Accounts)]
#[instruction(symbol: String)]
pub struct InitializeFundingRate<'info> {
    #[account(
        init,
        payer = authority,
        space = 8 + FundingRate::MAX_SIZE,
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
