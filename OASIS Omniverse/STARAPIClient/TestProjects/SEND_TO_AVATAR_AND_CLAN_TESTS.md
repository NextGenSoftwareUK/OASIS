# Send-to-Avatar and Send-to-Clan Tests (STAR API Client)

## Where the tests are

| Test type | Location | What's covered |
|-----------|----------|----------------|
| **Unit** | `NextGenSoftware.OASIS.STARAPI.Client.UnitTests/StarApiClientUnitTests.cs` | `SendItemToAvatarAsync_WhenNotInitialized_ReturnsNotInitialized`, `SendItemToClanAsync_WhenNotInitialized_ReturnsNotInitialized` – client returns NotInitialized when not initialized. No success-path or backend tests. |
| **Integration** | `NextGenSoftware.OASIS.STARAPI.Client.IntegrationTests/StarApiClientIntegrationTests.cs` | `SendItemToAvatar_Succeeds`, `SendItemToClan_Succeeds` – full flow against fake server or real API. Against real API they run only when env vars are set (see below). |
| **Test harness** | `NextGenSoftware.OASIS.STARAPI.Client.TestHarness/Program.cs` | Calls `SendItemToAvatarAsync` and `SendItemToClanAsync` after adding items; requires `STARAPI_SEND_TARGET_AVATAR` / `STARAPI_SEND_TARGET_CLAN` for real API. |

## Running integration tests against real API

- **Send-to-avatar**: Set `STARAPI_SEND_TARGET_AVATAR` to a valid target (username or avatar Id) that exists on the server. If unset, the test is skipped when not using the fake server.
- **Send-to-clan**: Set `STARAPI_SEND_TARGET_CLAN` to a valid clan name. If unset, the test is skipped when not using the fake server.
- **Timeout**: Send operations hit the backend (load/save avatar detail and optionally clan/holon). If the backend uses a slow provider (e.g. blockchain RPC), use a higher client timeout: set `STARAPI_TIMEOUT_SECONDS` (e.g. `60` or `90`) so `StarApiConfig.TimeoutSeconds` is set in the test client.

## Backend (ONODE / STAR API)

- **Endpoints**:  
  - Send-to-avatar: `POST /api/avatar/inventory/send-to-avatar`  
  - Send-to-clan: `POST /api/avatar/inventory/send-to-clan`
- **Implementation**:  
  - `AvatarController` → `AvatarManager.SendItemToAvatarAsync` / `SendItemToClanAsync`.  
  - Send-to-clan resolves clan by name via `ClanManager.LoadClanByNameAsync`, then `ClanManager.SendItemToClanAsync`.
- **Storage provider**: Both flows use **ProviderType.Default** (the current OASIS storage provider). They do **not** force Arbitrum; they use whatever is configured as the default (e.g. in OASIS DNA).  
  - Load/save path: `LoadAvatarDetailAsync` / `SaveAvatarDetailAsync` (avatar), and for clan also `LoadClanAsync` / `HolonManager.SaveHolonAsync`.  
  - Each of these calls `ProviderManager.SetAndActivateCurrentStorageProviderAsync(providerType)`. If the default provider is **ArbitrumOASIS**, then:
    - **“Not being able to activate Arbitrum”**: The Arbitrum provider’s `ActivateProviderAsync()` failed (e.g. missing/invalid connection string, chain private key, or RPC endpoint in OASIS DNA).
    - **Timeout**: The HTTP request from the client can time out (default 30s) if the backend is slow (e.g. Arbitrum RPC or multiple load/save rounds).

## Fixing “timeout” and “cannot activate Arbitrum” in Doom/Quake

1. **Backend (ONODE)**  
   - Ensure the **default storage provider** used for avatar/holon storage is correctly configured in OASIS DNA (e.g. `appsettings.json` or OASIS DNA config).  
   - If the default is Arbitrum: set `ArbitrumOASIS` connection string, chain private key, chain ID, and contract address. Confirm the Arbitrum RPC endpoint is reachable and responsive.  
   - Restart the API and check logs for “Failed to activate Arbitrum provider” to see the exact activation error.

2. **Client (Doom/Quake / STAR API client)**  
   - Increase timeout when initializing the STAR client: e.g. `StarApiConfig { ..., TimeoutSeconds = 60 }` so send-to-avatar and send-to-clan have enough time when the backend is slow.

3. **Verification**  
   - Run the test harness with `STARAPI_SEND_TARGET_AVATAR` and `STARAPI_SEND_TARGET_CLAN` set and, if needed, `STARAPI_TIMEOUT_SECONDS=60`.  
   - Run integration tests with the same env vars (and optional `STARAPI_TIMEOUT_SECONDS`) against your real API to confirm both send flows succeed.
