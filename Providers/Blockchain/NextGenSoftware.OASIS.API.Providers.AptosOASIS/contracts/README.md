# Aptos OASIS Smart Contract

This Move module provides on-chain storage for OASIS avatars, avatar details, holons, NFTs, and transactions on the Aptos blockchain.

## Deployment

1. Install Aptos CLI: https://aptos.dev/tools/aptos-cli/install-cli/
2. Initialize a new Move project:
   ```bash
   aptos move init --name oasis
   ```
3. Copy `oasis.move` to `sources/oasis.move`
4. Build the contract:
   ```bash
   aptos move compile
   ```
5. Test the contract:
   ```bash
   aptos move test
   ```
6. Publish the contract:
   ```bash
   aptos move publish --named-addresses oasis=<your-account-address>
   ```
7. Initialize the storage:
   ```bash
   aptos move run --function-id <your-account-address>::oasis::initialize
   ```
8. Update `_contractAddress` in `AptosOASIS.cs` with your account address.

## Functions

The contract provides the following entry and view functions that match the C# provider calls:

### Entry Functions:
- `initialize` - Initializes the OASIS storage
- `create_avatar` - Creates a new avatar
- `update_avatar` - Updates an existing avatar
- `delete_avatar` - Deletes an avatar
- `create_avatar_detail` - Creates avatar detail
- `create_holon` - Creates a holon
- `mint_nft` - Mints an NFT
- `transfer_nft` - Transfers an NFT
- `send_transaction` - Records a transaction

### View Functions:
- `get_avatar` - Get avatar by ID
- `get_avatar_count` - Get total avatar count
- `get_holon_count` - Get total holon count
- `get_nft_count` - Get total NFT count

## Storage Structure

The contract uses Aptos TableWithLength for storage:
- `avatars` - Avatar ID -> Avatar struct
- `avatar_details` - Avatar ID -> AvatarDetail struct
- `holons` - Holon ID -> Holon struct
- `nfts` - Token ID -> NFT struct
- `transactions` - Transaction ID -> Transaction struct

## Events

The contract emits the following events:
- `AvatarCreatedEvent` - When an avatar is created
- `AvatarUpdatedEvent` - When an avatar is updated
- `AvatarDeletedEvent` - When an avatar is deleted
- `HolonCreatedEvent` - When a holon is created
- `NFTMintedEvent` - When an NFT is minted
- `NFTTransferredEvent` - When an NFT is transferred
- `TransactionEvent` - When a transaction is recorded

