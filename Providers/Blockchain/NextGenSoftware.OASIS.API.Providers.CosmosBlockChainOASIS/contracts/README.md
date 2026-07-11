# Cosmos OASIS Smart Contract

This CosmWasm smart contract provides on-chain storage for OASIS avatars, avatar details, and holons on Cosmos SDK chains.

## Deployment

1. Install Rust and wasm32 target:
   ```bash
   rustup target add wasm32-unknown-unknown
   cargo install wasm-pack
   ```
2. Build the contract:
   ```bash
   cd contracts/oasis
   cargo wasm
   ```
3. Optimize the wasm:
   ```bash
   wasm-opt -Os target/wasm32-unknown-unknown/release/oasis.wasm -o oasis-optimized.wasm
   ```
4. Deploy to your Cosmos chain:
   ```bash
   wasmd tx wasm store oasis-optimized.wasm --from mykey --gas auto --gas-adjustment 1.3
   ```
5. Instantiate the contract:
   ```bash
   wasmd tx wasm instantiate <code_id> '{}' --from mykey --label "oasis" --admin <admin_address>
   ```
6. Update `_contractAddress` in `CosmosBlockChainOASIS.cs` with the instantiated contract address.

## Functions

The contract provides the following execute and query messages:

### Execute Messages:
- `create_avatar` - Creates a new avatar
- `save_avatar_detail` - Saves avatar detail information
- `delete_avatar` - Deletes an avatar (soft or hard delete)
- `save_holon` - Saves a holon
- `delete_holon` - Deletes a holon

### Query Messages:
- `get_avatar_by_id` - Get avatar by ID
- `get_avatar_by_username` - Get avatar by username
- `get_avatar_by_email` - Get avatar by email
- `get_all_avatars` - Get all avatars
- `get_avatar_detail` - Get avatar detail by ID
- `get_avatar_detail_by_username` - Get avatar detail by username
- `get_avatar_detail_by_email` - Get avatar detail by email
- `get_all_avatar_details` - Get all avatar details
- `get_holon` - Get holon by ID
- `get_holons_for_parent` - Get holons by parent ID
- `get_holons_by_metadata` - Get holons by metadata key/value
- `get_all_holons` - Get all holons
- `search` - Search holons and avatars

## Storage

The contract uses CosmWasm storage maps:
- `AVATARS` - Avatar ID -> Avatar struct
- `AVATARS_BY_USERNAME` - Username -> Avatar ID
- `AVATARS_BY_EMAIL` - Email -> Avatar ID
- `AVATAR_DETAILS` - Avatar ID -> AvatarDetail struct
- `AVATAR_DETAILS_BY_USERNAME` - Username -> AvatarDetail ID
- `AVATAR_DETAILS_BY_EMAIL` - Email -> AvatarDetail ID
- `HOLONS` - Holon ID -> Holon struct
- `HOLONS_BY_PARENT` - Parent ID -> Vector of Holon IDs

