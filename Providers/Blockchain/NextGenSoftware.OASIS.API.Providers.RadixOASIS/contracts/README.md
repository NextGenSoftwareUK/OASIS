# OASIS Storage Scrypto Blueprint

This directory contains the Scrypto blueprint for OASIS storage on Radix blockchain.

## Overview

The `oasis_storage` blueprint provides persistent storage for OASIS Avatars, AvatarDetails, and Holons on the Radix blockchain. It mirrors the functionality of the ArbitrumOASIS Solidity smart contract.

## Structure

```
contracts/
├── Cargo.toml           # Rust package configuration
├── src/
│   └── oasis_storage.rs # Main Scrypto blueprint
└── README.md            # This file
```

## Building

To build the Scrypto package:

```bash
cd contracts
scrypto build
```

This will compile the blueprint and create a package that can be published and instantiated.

## Deploying

1. **Publish the package**:
   ```bash
   resim publish .
   ```

2. **Instantiate the component**:
   ```bash
   resim call-function <PACKAGE_ADDRESS> OasisStorage instantiate
   ```

3. **Save the component address** to your `OASIS_DNA.json`:
   ```json
   "RadixOASIS": {
     "ComponentAddress": "component_rdx1..."
   }
   ```

## Component Methods

### Avatar Operations

- `create_avatar(entity_id: u64, avatar_json: String, username: String, email: String) -> u64`
  - Creates a new avatar with the given entity ID and JSON data
  - Returns the entity ID

- `update_avatar(entity_id: u64, avatar_json: String)`
  - Updates an existing avatar

- `get_avatar(entity_id: u64) -> Option<String>`
  - Retrieves avatar JSON by entity ID

- `get_avatar_by_username(username: String) -> Option<String>`
  - Retrieves avatar JSON by username

- `get_avatar_by_email(email: String) -> Option<String>`
  - Retrieves avatar JSON by email

- `delete_avatar(entity_id: u64)`
  - Deletes an avatar

### AvatarDetail Operations

- `create_avatar_detail(entity_id: u64, avatar_detail_json: String) -> u64`
  - Creates a new avatar detail

- `get_avatar_detail(entity_id: u64) -> Option<String>`
  - Retrieves avatar detail JSON by entity ID

### Holon Operations

- `create_holon(entity_id: u64, holon_json: String, provider_key: String) -> u64`
  - Creates a new holon

- `update_holon(entity_id: u64, holon_json: String)`
  - Updates an existing holon

- `get_holon(entity_id: u64) -> Option<String>`
  - Retrieves holon JSON by entity ID

- `get_holon_by_provider_key(provider_key: String) -> Option<String>`
  - Retrieves holon JSON by provider key

- `delete_holon(entity_id: u64)`
  - Deletes a holon

### Query Operations

- `get_avatar_count() -> u64`
- `get_avatar_detail_count() -> u64`
- `get_holon_count() -> u64`

## Storage Model

The component uses KeyValueStore for efficient storage:

- `avatars`: EntityId -> Avatar JSON
- `avatar_details`: EntityId -> AvatarDetail JSON
- `holons`: EntityId -> Holon JSON
- `username_to_avatar_id`: Username -> EntityId (index)
- `email_to_avatar_id`: Email -> EntityId (index)
- `provider_key_to_holon_id`: ProviderKey -> EntityId (index)

## Integration

Once deployed, the component address must be configured in:

1. `OASIS_DNA.json` - Add `ComponentAddress` field
2. `RadixOASISConfig` - Component address property
3. `RadixOASIS.cs` - Use component address in method calls

See `RADIX_PROVIDER_IMPLEMENTATION_GAP.md` for full integration details.

