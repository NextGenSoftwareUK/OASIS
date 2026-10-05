# Adding a New OASIS Provider

When we add a new chain provider (e.g., Starknet), a few shared core pieces must be updated so the rest of the system can discover and use it uniformly.

## 1. Provider implementation
- Create a `NextGenSoftware.OASIS.API.Providers.<Name>OASIS` project under `Providers/Blockchain` with a `.csproj`, README, and provider class deriving from `OASISStorageProviderBase` (or the appropriate base for NFT/network providers).
- Implement the required interfaces, stub the abstract methods with `NotImplemented`, and add chain-specific helpers (`CreateAtomicSwapIntentAsync`, bridge hooks, etc.).
- Provide a `StarknetBridge` implementation if the chain participates in the cross-chain bridge layer.

## 2. Provider metadata
- Update `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/ProviderType.cs` to add the enum value.
- Document the provider in `OASIS Architecture/NextGenSoftware.OASIS.API.Core/OASIS_DNA.json` (typically under `StorageProviders` or the relevant configuration section).
- Note the new provider in any implementation status docs (`Providers/Blockchain/IMPLEMENTATION_STATUS.md`).

## 3. Registry & bootstrapping
- Register the provider in `ProviderRegistry`/`ProviderManagerNew` via `ProviderConfigurator`, so the provider lists, auto-failover rules, and selector logic see it.
- Call `ProviderManagerNew.RegisterStorageProvider(providerType, provider)` (or the equivalent registry API used by ONODE/bootloader) at startup so the provider can be activated.
- Ensure the `ProviderController` endpoints can return the new provider type (the controller already reads lists from the registry).

## 4. Bridge wiring (if applicable)
- Modify `CrossChainBridgeManager` to recognize any new private bridge token pair.
- Create or extend an `IOASISBridge` implementation for the chain (e.g., `StarknetBridge`) and register it in the `_bridgeMap`.
- Hook the new bridge into any atomic swap flows that call `bridgeApi`/`CreateBridgeOrderAsync`.

## 5. API & UI surface
- Keep the `zypherpunk-wallet-ui` flows using `lib/api/bridgeApi.ts`, `stablecoinApi.ts`, etc., pointing at `/api/v1/bridge/...` endpoints; backend controller implementations under `zypherpunk/stablecoin/backend` only need to route to the new provider.
- Add the provider to `lib/types.ts` (if a `ProviderType` enum copy exists in the frontend) so the UI can render wallet lists for the new chain.
- Document any new endpoints or UX surfaces in README/plan docs.

## 6. Testing & activation
- Use `ProviderManager.SetAndActivateCurrentStorageProvider` or the ONODE `api/provider` endpoints to switch to the new provider for tests.
- Confirm the provider shows up in `ProviderManagerNew.Instance.GetAvailableProviders()` and that bridge/stablecoin controllers can reach it.
- If there’s an associated bridge, run the `CrossChainBridgeManager` tests and ensure the `StarknetBridge` stub returns expected responses before wiring real RPC calls.

Keeping these steps documented makes future provider additions predictable and consistent.

## Recent example: Starknet onboarding

- Scaffolded `StarknetOASIS`/`StarknetBridge` under `Providers/Blockchain`, then added the enum entry plus socialized the provider in `Docs/PROVIDER_ONBOARDING.md`.
- Injected the new bridge into `ONODE/.../BridgeService` so the `"STARKNET"` key hits the placeholder service while the `CrossChainBridgeManager` already understands ZEC↔Starknet as a private pair.
- Wired `OASISProviderManager` to register `StarknetOASIS` when requested and extended ONODE settings (`OASISSettings`, `appsettings.json`, `OASIS_DNA.json`) with RPC/network defaults for the Starknet provider.
- Added a `--test-starknet-bridge` mode to `NextGenSoftware.OASIS.API.Core.TestHarness` that exercises `StarknetBridge` with a fake RPC client so backend wiring can be smoke-tested (`dotnet run --project .../TestHarness -- --test-starknet-bridge`).

