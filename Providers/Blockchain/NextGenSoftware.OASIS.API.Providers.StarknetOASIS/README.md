# StarknetOASIS Provider

This provider wraps Starknet tooling inside the OASIS storage provider abstraction. It is the foundation for ZEC ↔ Starknet atomic swap flows, Starknet account management, and any future Starknet-specific privacy helpers.

## Goals

- Provide the same interface surface as existing providers so `WalletManager`, bridges, and UI flows can treat Starknet wallets uniformly.
- Offer helper APIs to reserve Starknet assets, create atomic swap intents, and surface Starknet status to Holons.
- Keep activation/deactivation lightweight while we iterate on the full Starknet client integration.

## Development notes

- Implementations initially return stub errors so the rest of the stack can integrate incrementally.
- The provider is wired into `OASIS_DNA.json` and the `ProviderType` enum so it can be resolved by name.

