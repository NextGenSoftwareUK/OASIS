# Polkadot OASIS Smart Contract

This ink! smart contract provides on-chain storage for OASIS avatars, avatar details, and holons on Polkadot/Substrate chains.

## Deployment

1. Install ink! toolchain: https://use.ink/getting-started/setup
2. Build the contract:
   ```bash
   cargo contract build
   ```
3. Deploy the contract to your Substrate chain:
   ```bash
   cargo contract instantiate --constructor new --suri //Alice --execute
   ```
4. Update `_contractAddress` in `PolkadotOASIS.cs` with the deployed contract address.

## Functions

The contract provides the following messages that match the C# provider RPC calls:

### View Functions (called via `state_call` RPC):
- `get_avatar_by_email` - Maps to `Oasis_getAvatarByEmail`
- `get_avatar_by_username` - Maps to `Oasis_getAvatarByUsername`
- `get_all_avatars` - Maps to `Oasis_getAllAvatars`
- `get_avatar_detail` - Maps to `Oasis_getAvatarDetail`
- `get_avatar_detail_by_username` - Maps to `Oasis_getAvatarDetailByUsername`
- `get_avatar_detail_by_email` - Maps to `Oasis_getAvatarDetailByEmail`
- `get_all_avatar_details` - Maps to `Oasis_getAllAvatarDetails`
- `get_holon` - Maps to `Oasis_getHolon`
- `get_holon_by_provider_key` - Maps to `Oasis_getHolonByProviderKey`
- `get_holons_for_parent` - Maps to `Oasis_getHolonsForParent`
- `get_holons_by_metadata` - Maps to `Oasis_getHolonsByMetaData`
- `get_all_holons` - Maps to `Oasis_getAllHolons`
- `search` - Maps to `Oasis_search`
- `get_nft` - Maps to `Oasis_getNFT`

### Transaction Functions (called via extrinsics):
- `save_avatar_detail` - Maps to `Oasis_saveAvatarDetail`
- `delete_avatar` - Maps to `Oasis_deleteAvatar`
- `save_holon` - Maps to `Oasis_saveHolon`
- `delete_holon` - Maps to `Oasis_deleteHolon`

## Storage

The contract uses ink! storage mappings:
- `avatars` - Avatar ID -> Avatar struct
- `avatars_by_username` - Username -> Avatar ID
- `avatars_by_email` - Email -> Avatar ID
- `avatar_details` - Avatar ID -> AvatarDetail struct
- `avatar_details_by_username` - Username -> AvatarDetail ID
- `holons` - Holon ID -> Holon struct
- `holons_by_parent` - Parent ID -> Vector of Holon IDs

