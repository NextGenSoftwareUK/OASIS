# Sui OASIS Smart Contract

This Move module provides on-chain storage for OASIS avatars, avatar details, and holons on the Sui blockchain.

## Deployment

1. Install Sui CLI: https://docs.sui.io/build/install
2. Build the contract:
   ```bash
   sui move build
   ```
3. Publish the contract:
   ```bash
   sui client publish --gas-budget 100000000
   ```
4. Update `_contractAddress` in `SuiOASIS.cs` with the published package ID.

## Functions

The contract provides the following entry and view functions that match the C# provider calls:

- `create_avatar` - Creates a new avatar
- `save_avatar_detail` - Saves avatar detail information
- `save_holon` - Saves a holon
- `get_avatar_by_username` - View function to get avatar by username
- `get_avatar_by_email` - View function to get avatar by email
- `get_holons_by_parent` - View function to get holons by parent ID
- `get_holon_by_id` - View function to get holon by ID
- `search_holons` - View function to search holons

## Storage Structure

The contract uses Sui's object model with:
- `Avatar` objects - Shareable objects for avatars
- `AvatarDetail` objects - Shareable objects for avatar details
- `Holon` objects - Shareable objects for holons
- `OasisStorage` - Shared object containing indexes and mappings

