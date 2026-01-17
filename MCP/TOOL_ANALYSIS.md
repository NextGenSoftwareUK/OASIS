# MCP Tools Analysis - Duplicates & Performance Optimization

## Current Tool Count
- **OASIS Tools**: 77
- **Smart Contract Tools**: 5
- **Total**: 82 tools

## Duplicate/Overlapping Tools

### 1. Avatar Retrieval (3 tools - can consolidate to 1-2)
- `oasis_get_avatar` - Basic avatar info by ID/username/email
- `oasis_get_avatar_detail` - Detailed avatar info by ID/username/email
- `oasis_get_avatar_portrait` - Avatar portrait by ID/username/email

**Recommendation**: Keep `oasis_get_avatar_detail` (most comprehensive), remove `oasis_get_avatar` and `oasis_get_avatar_portrait` OR merge portrait into detail.

### 2. All Avatars (2 tools - can consolidate to 1)
- `oasis_get_all_avatars` - All avatars (admin only)
- `oasis_get_all_avatar_details` - All avatar details (admin only)

**Recommendation**: Keep `oasis_get_all_avatar_details` (more comprehensive), remove `oasis_get_all_avatars`.

### 3. Wallet Creation (4 tools - can consolidate to 2-3)
- `oasis_create_wallet` - Generic wallet creation
- `oasis_create_wallet_full` - Full wallet creation with options
- `oasis_create_solana_wallet` - Solana-specific wallet creation
- `oasis_create_ethereum_wallet` - Ethereum-specific wallet creation

**Recommendation**: Keep `oasis_create_solana_wallet` and `oasis_create_ethereum_wallet` (most specific), remove `oasis_create_wallet` and `oasis_create_wallet_full` OR keep only `oasis_create_wallet_full` as the unified method.

### 4. Provider Wallets (3 tools - can consolidate to 1)
- `oasis_get_provider_wallets` - By avatarId
- `oasis_get_provider_wallets_by_username` - By username
- `oasis_get_provider_wallets_by_email` - By email

**Recommendation**: Keep `oasis_get_provider_wallets` and add username/email as optional parameters, remove the other two.

### 5. NFT Lookup (4 tools - can consolidate to 2-3)
- `oasis_get_nft` - By NFT ID
- `oasis_get_nft_by_hash` - By hash
- `oasis_get_nfts` - All NFTs for avatar
- `oasis_get_geo_nfts` - All GeoNFTs for avatar

**Recommendation**: Keep all (different use cases), but consider merging `oasis_get_nft` and `oasis_get_nft_by_hash` into one with optional hash parameter.

### 6. All NFTs (2 tools - can consolidate to 1)
- `oasis_get_all_nfts` - All NFTs (admin only)
- `oasis_get_all_geo_nfts` - All GeoNFTs (admin only)

**Recommendation**: Keep both (different NFT types), but consider adding a type filter parameter to one tool.

### 7. Holon Loading (3 tools - can consolidate to 1-2)
- `oasis_search_holons` - Search holons
- `oasis_load_holons_for_parent` - Load by parent
- `oasis_load_all_holons` - Load all (admin only)

**Recommendation**: Keep `oasis_search_holons` (most flexible), remove `oasis_load_holons_for_parent` (can use search with parentId filter), keep `oasis_load_all_holons` if admin-only is needed.

### 8. Search Tools (5 tools - can consolidate to 1-2)
- `oasis_basic_search` - Basic search
- `oasis_advanced_search` - Advanced search with filters
- `oasis_search_avatars` - Avatar-specific search
- `oasis_search_nfts` - NFT-specific search
- `oasis_search_files` - File-specific search

**Recommendation**: Keep `oasis_advanced_search` (can handle all with entityType filter), remove `oasis_basic_search`, `oasis_search_avatars`, `oasis_search_nfts`, `oasis_search_files`.

### 9. Karma Weighting (4 tools - keep all, but consider consolidation)
- `oasis_get_positive_karma_weighting` - Get positive karma weighting
- `oasis_get_negative_karma_weighting` - Get negative karma weighting
- `oasis_vote_positive_karma_weighting` - Vote on positive karma
- `oasis_vote_negative_karma_weighting` - Vote on negative karma

**Recommendation**: Keep all (different operations), but consider merging get/vote pairs into single tools with operation parameter.

### 10. NFT Mint Address (2 tools - keep both)
- `oasis_get_nfts_for_mint_address` - Regular NFTs
- `oasis_get_geo_nfts_for_mint_address` - GeoNFTs

**Recommendation**: Keep both (different NFT types).

## Admin-Only Tools (Consider Removing or Gating)

These tools require admin authentication and are rarely used:
- `oasis_get_all_avatars` - **REMOVE** (use `oasis_get_all_avatar_details`)
- `oasis_get_all_avatar_details` - Keep if admin functionality needed
- `oasis_get_all_nfts` - Keep if admin functionality needed
- `oasis_get_all_geo_nfts` - Keep if admin functionality needed
- `oasis_load_all_holons` - Keep if admin functionality needed
- `oasis_get_all_avatar_names` - **REMOVE** (rarely used, can use search)

## Rarely Used Tools (Consider Removing)

- `oasis_get_avatar_portrait` - **REMOVE** (merge into `oasis_get_avatar_detail`)
- `oasis_get_karma_akashic_records` - Keep if needed for karma system
- `oasis_get_wallet_analytics` - Keep if analytics are used
- `oasis_get_wallet_tokens` - Keep if needed
- `oasis_get_portfolio_value` - Keep if needed

## Performance Impact

**High Impact Removals** (reduce tool list size significantly):
1. Remove duplicate avatar tools: -2 tools
2. Remove duplicate wallet creation: -2 tools
3. Remove duplicate provider wallets: -2 tools
4. Remove duplicate search tools: -4 tools
5. Remove duplicate holon loading: -1 tool
6. Remove admin-only duplicates: -2 tools
7. Remove rarely used: -2 tools

**Total Potential Reduction**: ~15 tools (from 82 to ~67)

## Recommended Actions

### Phase 1: Remove Clear Duplicates (High Priority)
1. Remove `oasis_get_avatar` (use `oasis_get_avatar_detail`)
2. Remove `oasis_get_avatar_portrait` (merge into `oasis_get_avatar_detail`)
3. Remove `oasis_get_all_avatars` (use `oasis_get_all_avatar_details`)
4. Remove `oasis_get_all_avatar_names` (use search)
5. Remove `oasis_get_provider_wallets_by_username` (add to main tool)
6. Remove `oasis_get_provider_wallets_by_email` (add to main tool)
7. Remove `oasis_basic_search` (use `oasis_advanced_search`)
8. Remove `oasis_search_avatars` (use `oasis_advanced_search` with entityType)
9. Remove `oasis_search_nfts` (use `oasis_advanced_search` with entityType)
10. Remove `oasis_search_files` (use `oasis_advanced_search` with entityType)
11. Remove `oasis_load_holons_for_parent` (use `oasis_search_holons` with parentId)

**Phase 1 Reduction**: -11 tools

### Phase 2: Consolidate Similar Tools (Medium Priority)
1. Merge `oasis_get_nft` and `oasis_get_nft_by_hash` (add optional hash parameter)
2. Consolidate wallet creation tools (keep only specific ones or one unified)
3. Consider merging karma weighting get/vote pairs

**Phase 2 Reduction**: -2 to -4 tools

### Phase 3: Review Admin Tools (Low Priority)
- Evaluate if admin-only tools are actually needed
- Consider gating them behind a flag or separate MCP server

## Summary

**Current**: 82 tools
**After Phase 1**: ~71 tools (-11)
**After Phase 2**: ~67-69 tools (-13 to -15 total)
**Potential Final**: ~65-70 tools (20-25% reduction)

This would improve:
- Tool discovery time
- MCP server startup time
- Memory usage
- Tool selection accuracy in AI models
