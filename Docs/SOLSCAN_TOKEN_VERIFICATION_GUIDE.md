# Solscan Token Verification Guide

## Overview

Getting a token "verified on Solscan" means having your token appear on Solscan with:
- ✅ Verified badge
- ✅ Token logo/icon
- ✅ Correct name and symbol
- ✅ Token metadata (description, website, social links)

This is different from smart contract verification. It's about getting your token listed in the Solana Token List, which Solscan uses to display token information.

---

## Method 1: Submit to Solana Token List (Recommended)

Solscan uses the **Solana Token List** (community-maintained) to display token information. This is the primary way to get verified.

### Step 1: Prepare Your Token Information

Before submitting, you need:

- **Token Mint Address** (e.g., `EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v`)
- **Token Name** (e.g., "My Awesome Token")
- **Token Symbol** (e.g., "MAT")
- **Token Logo** (512x512 PNG with transparent background)
- **Decimals** (usually 6 or 9 for most tokens)
- **Website URL** (optional but recommended)
- **Social Links** (Twitter, Telegram, Discord - optional)
- **Description** (what your token does)

### Step 2: Fork the Repository

1. Go to: https://github.com/solana-labs/token-list
2. Click **"Fork"** to create your own copy

### Step 3: Add Your Token Entry

1. Navigate to the `src/tokens` folder in your forked repository
2. Find the appropriate file (usually `solana.tokenlist.json` or create a new file if needed)
3. Add your token entry in the correct format:

```json
{
  "chainId": 101,
  "address": "YOUR_TOKEN_MINT_ADDRESS",
  "symbol": "YOUR_SYMBOL",
  "name": "Your Token Name",
  "decimals": 9,
  "logoURI": "https://your-domain.com/logo.png",
  "tags": ["your-tag"],
  "extensions": {
    "website": "https://your-website.com",
    "twitter": "https://twitter.com/yourhandle",
    "telegram": "https://t.me/yourchannel",
    "discord": "https://discord.gg/yourserver"
  }
}
```

**Important Notes:**
- `chainId: 101` = Mainnet-Beta
- `chainId: 102` = Testnet
- `chainId: 103` = Devnet
- The `logoURI` must be publicly accessible HTTPS URL
- Logo should be 512x512 PNG with transparent background

### Step 4: Upload Your Logo

1. Create a folder in `assets/mainnet/YOUR_TOKEN_MINT_ADDRESS/` (use full mint address)
2. Add your logo file (e.g., `logo.png`)
3. Use the relative path in your token entry: `logoURI: "https://raw.githubusercontent.com/solana-labs/token-list/main/assets/mainnet/YOUR_TOKEN_MINT_ADDRESS/logo.png"`

### Step 5: Submit Pull Request

1. Commit your changes
2. Push to your fork
3. Go back to the original repository
4. Click **"Pull Requests"** → **"New Pull Request"**
5. Select your fork as the source
6. Fill out the PR template with:
   - Token mint address
   - Brief description
   - Links to your website/socials
7. Submit the PR

### Step 6: Wait for Review

- The Solana team reviews PRs periodically
- This can take several days to weeks
- Once merged, Solscan will automatically update (usually within 24-48 hours)

---

## Method 2: Contact Solscan Directly

If you need faster verification or have special requirements:

1. **Go to Solscan**: https://solscan.io
2. **Navigate to your token page**: `https://solscan.io/token/YOUR_TOKEN_MINT_ADDRESS`
3. **Look for verification options** or contact form
4. **Email support**: Some users report success emailing Solscan support directly

**Information to include:**
- Token mint address
- Token name and symbol
- Website and social media links
- Why you need verification
- Proof of ownership (ability to sign messages from the mint authority)

---

## Method 3: Ensure On-Chain Metadata is Correct

Some explorers pull metadata directly from on-chain data. Ensure your token has proper metadata:

### For SPL Tokens (Regular Fungible Tokens)

If you created your token using `spl-token`, metadata should be set via:
- Token extensions (if using SPL Token Extensions)
- Metadata URI in associated accounts

### For Metaplex Tokens

If using Metaplex, ensure:
- Token metadata account is properly initialized
- Name, symbol, and URI are set correctly
- Creator is verified

```bash
# Check your token metadata on-chain
solana account YOUR_TOKEN_MINT_ADDRESS --output json
```

---

## Quick Checklist

Before submitting, ensure:

- [ ] Token is live on mainnet (not devnet/testnet for mainnet listing)
- [ ] Token mint address is correct
- [ ] Logo is 512x512 PNG with transparent background
- [ ] Logo URL is publicly accessible via HTTPS
- [ ] Token name and symbol match on-chain data
- [ ] Decimals match on-chain configuration
- [ ] Website/social links are accurate
- [ ] Token has some liquidity or trading activity (not always required but helps)

---

## Verification Timeline

- **Solana Token List PR**: 1-4 weeks (varies based on review queue)
- **After PR merge**: 24-48 hours for Solscan to update
- **Direct contact**: Varies, can be faster if urgent

---

## Common Issues & Solutions

### Issue: PR gets rejected
**Solution**: Ensure all required fields are correct, logo follows guidelines, and token information matches on-chain data.

### Issue: Logo doesn't display
**Solution**: Check that:
- Logo URL is accessible
- Logo is proper format (PNG)
- Logo path in PR matches actual file location

### Issue: Token appears but not verified
**Solution**: "Verified" badge may require additional criteria beyond just being in the token list. Contact Solscan support.

### Issue: Token info appears but logo is wrong
**Solution**: Update the `logoURI` in a new PR, or contact support if you need immediate changes.

---

## Alternative: Community Token Lists

While Solscan primarily uses the official Solana Token List, there are community-maintained lists:
- **Jupiter Token List**: https://github.com/jup-ag/token-list
- **Raydium Token List**: Uses official list

These may have different verification processes.

---

## Important Notes

1. **Verification ≠ Legitimacy**: Being on Solscan doesn't mean the token is safe or legitimate. Users should always DYOR.

2. **Multiple Chains**: This guide is for Solana mainnet. If you need testnet/devnet verification, use `chainId: 102` or `103`.

3. **Fees**: Listing in the Solana Token List is free. Be wary of services charging for "verification" - they may just be submitting to the list for you.

4. **Updates**: If your token information changes, submit a new PR with updated information.

---

## Need Help?

If you're using OASIS infrastructure and need help with token creation or metadata setup, check:
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/` for token minting code
- Your token's on-chain metadata using Solana RPC calls


