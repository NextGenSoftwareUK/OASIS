# OpenAPI Endpoint Inventory

This file is **generated** from the live OpenAPI spec. Do not edit by hand.

**Source:** [OpenAPI spec](http://api.oasisweb4.com/swagger/v1/swagger.json)
**Swagger UI:** [Interactive docs](http://api.oasisweb4.com/swagger/index.html)

**Generated:** 2026-01-28T13:50:47.369Z
**Total endpoints:** 600

---

## A2A

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/A2A/agent-card/{agentId}` | Get Agent Card for an agent (Official A2A Protocol) |
| GET | `/api/A2A/agent-card` | Get Agent Card for authenticated agent |
| POST | `/api/A2A/agent/{agentId}/mint-nft` | Mint an NFT representing ownership of an agent (makes agent tradable) |
| GET | `/api/A2A/agent/{agentId}/nft` | Get NFT information for an agent |
| GET | `/api/A2A/agent/{agentId}/owner` | Get the owner of an agent |
| POST | `/api/A2A/agent/capabilities` | Register agent capabilities |
| POST | `/api/A2A/agent/link-to-user` | Link an agent to a user avatar (owner) |
| POST | `/api/A2A/agent/register-service` | Register A2A agent as SERV service |
| POST | `/api/A2A/agent/unlink-from-user` | Unlink an agent from its owner |
| GET | `/api/A2A/agents/by-owner/{ownerAvatarId}` | Get all agents owned by a user |
| GET | `/api/A2A/agents/by-service/{serviceName}` | Find agents by service |
| GET | `/api/A2A/agents/discover-onet` |  |
| GET | `/api/A2A/agents/discover-serv` | Discover agents via SERV infrastructure (legacy endpoint - redirects to discover |
| GET | `/api/A2A/agents` | List all available agents (Agent Cards) |
| POST | `/api/A2A/jsonrpc` | JSON-RPC 2.0 endpoint - Main A2A Protocol endpoint |
| POST | `/api/A2A/karma/award` | Award karma for service completion |
| GET | `/api/A2A/karma` | Get karma for the authenticated agent |
| POST | `/api/A2A/messages/{messageId}/process` | Mark message as processed |
| GET | `/api/A2A/messages` | Get pending A2A messages for authenticated agent |
| GET | `/api/A2A/mnee/balance` | Get MNEE balance for an agent |
| POST | `/api/A2A/mnee/payment` | Send MNEE payment request between agents |
| POST | `/api/A2A/nft/reputation` | Create a reputation NFT for the authenticated agent |
| POST | `/api/A2A/nft/service-certificate` | Create a service completion certificate NFT |
| GET | `/api/A2A/openserv/discover` | Discover agents from OpenSERV platform (bidirectional discovery) |
| POST | `/api/A2A/openserv/register-oasis-agent` | Register OASIS A2A agent with OpenSERV platform (bidirectional discovery) |
| POST | `/api/A2A/openserv/register` | Register OpenSERV agent |
| GET | `/api/A2A/serv/balance` | Get SERV balance for an agent |
| POST | `/api/A2A/serv/payment` | Send SERV payment request between agents |
| POST | `/api/A2A/task/complete` | Complete a delegated task |
| POST | `/api/A2A/task/delegate` | Delegate a task to another agent |
| GET | `/api/A2A/tasks` | Get tasks for the authenticated agent |
| POST | `/api/A2A/workflow/execute` | Execute AI workflow via OpenSERV |

## AI

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/AI/chat` | Chat with OpenAI for calibration: refine NFT ideas, get suggestions, and prepare |
| POST | `/api/AI/parse-intent` | Parse user intent from natural language input using OpenAI. Converts conversati |

## Avatar

| Method | Path | Summary |
|--------|------|---------|
| PUT | `/api/Avatar/{avatarId}/sessions/{sessionId}` | Update an existing session (OASIS SSO System) |
| POST | `/api/Avatar/{avatarId}/sessions/logout-all` | Logout avatar from all sessions (OASIS SSO System) |
| POST | `/api/Avatar/{avatarId}/sessions/logout` | Logout avatar from specific sessions (OASIS SSO System) |
| GET | `/api/Avatar/{avatarId}/sessions/stats` | Get session statistics for an avatar (OASIS SSO System) |
| GET | `/api/Avatar/{avatarId}/sessions` | Get all active sessions for a specific avatar (OASIS SSO System) |
| POST | `/api/Avatar/{avatarId}/sessions` | Create a new session for an avatar (OASIS SSO System) |
| DELETE | `/api/Avatar/{id}/{providerType}/{setGlobally}` | Delete the given avatar using their id. Only works for logged in users. Use Aut |
| DELETE | `/api/Avatar/{id}` | Delete the given avatar using their id. Only works for logged in users. Use Aut |
| POST | `/api/Avatar/add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}` | Add positive karma to the given avatar. karmaType = The type of positive karma,  |
| POST | `/api/Avatar/add-karma-to-avatar/{avatarId}` | Add positive karma to the given avatar. karmaType = The type of positive karma,  |
| POST | `/api/Avatar/authenticate-token/{JWTToken}/{providerType}/{setGlobally}` | Authenticate and log in using the given JWT Token. Pass in the provider you wis |
| POST | `/api/Avatar/authenticate-token/{JWTToken}` | Authenticate and log in using the given JWT Token. |
| POST | `/api/Avatar/authenticate/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{AutoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Authenticate and log in using the given avatar credentials.  Pass in the provid |
| POST | `/api/Avatar/authenticate` | Authenticate and log in using avatar credentials. |
| DELETE | `/api/Avatar/delete-by-email/{email}/{providerType}/{setGlobally}` | Delete the given avatar using their email. Only works for logged in users. Use  |
| DELETE | `/api/Avatar/delete-by-email/{email}` | Delete the given avatar using their email. Only works for logged in users. Use  |
| DELETE | `/api/Avatar/delete-by-username/{username}/{providerType}/{setGlobally}` | Delete the given avatar using their username. Only works for logged in users. U |
| DELETE | `/api/Avatar/delete-by-username/{username}` | Delete the given avatar using their username. Only works for logged in users. U |
| POST | `/api/Avatar/forgot-password/{providerType}/{setGlobally}` | This will send a password reset email allowing the user to reset their password. |
| POST | `/api/Avatar/forgot-password` | This will send a password reset email allowing the user to reset their password. |
| GET | `/api/Avatar/get-all-avatar-details/{providerType}/{setGlobally}` | Get's all the avatar details within The OASIS. Only works for logged in & authe |
| GET | `/api/Avatar/get-all-avatar-details` | Get's all the avatar details within The OASIS. Only works for logged in & authe |
| GET | `/api/Avatar/get-all-avatar-names-grouped-by-name/{includeUsernames}/{includeIds}/{providerType}/{setGlobally}` | Get's a list of all of the avatar names within The OASIS along with their respec |
| GET | `/api/Avatar/get-all-avatar-names-grouped-by-name/{includeUsernames}/{includeIds}` | Get's a list of all of the avatar names within The OASIS along with their respec |
| GET | `/api/Avatar/get-all-avatar-names/{includeUsernames}/{includeIds}/{providerType}/{setGlobally}` | Get's a list of all of the avatar names within The OASIS. Only works for logged |
| GET | `/api/Avatar/get-all-avatar-names/{includeUsernames}/{includeIds}` | Get's a list of all of the avatar names within The OASIS. Only works for logged |
| GET | `/api/Avatar/get-all-avatars/{providerType}/{setGlobally}` | Get's all avatars within The OASIS.  Only works for logged in & authenticated W |
| GET | `/api/Avatar/get-all-avatars` | Get's all avatars within The OASIS. Only works for logged in & authenticated Wi |
| GET | `/api/Avatar/get-avatar-detail-by-email/{email}/{providerType}/{setGlobally}` | Get's the avatar's details for a given email. Contains their address, DOB, Karma |
| GET | `/api/Avatar/get-avatar-detail-by-email/{email}` | Get's the avatar's details for a given email. Contains their address, DOB, Karma |
| GET | `/api/Avatar/get-avatar-detail-by-id/{id}/{providerType}/{setGlobally}` | Get's the avatar's details for a given id. Contains their address, DOB, Karma, X |
| GET | `/api/Avatar/get-avatar-detail-by-id/{id}` | Get's the avatar's details for a given id. Contains their address, DOB, Karma, X |
| GET | `/api/Avatar/get-avatar-detail-by-username/{username}/{providerType}/{setGlobally}` | Get's the avatar's details for a given username. Contains their address, DOB, Ka |
| GET | `/api/Avatar/get-avatar-detail-by-username/{username}` | Get's the avatar's details for a given username. Contains their address, DOB, Ka |
| GET | `/api/Avatar/get-avatar-portrait-by-email/{email}/{providerType}/{setGlobally}` | Get's the avatar's portrait (2D Image) using their email. Pass in the provider y |
| GET | `/api/Avatar/get-avatar-portrait-by-email/{email}` | Get's the avatar's portrait (2D Image) using their email. |
| GET | `/api/Avatar/get-avatar-portrait-by-username/{username}/{providerType}/{setGlobally}` | Get's the avatar's portrait (2D Image) using their username. Pass in the provide |
| GET | `/api/Avatar/get-avatar-portrait-by-username/{username}` | Get's the avatar's portrait (2D Image) using their username. Only works for log |
| GET | `/api/Avatar/get-avatar-portrait/{id}/{providerType}/{setGlobally}` | Get's the avatar's portrait (2D Image) using their id.  Only works for logged i |
| GET | `/api/Avatar/get-avatar-portrait/{id}` | Get's the avatar's portrait (2D Image) using their id. Pass in the provider you  |
| GET | `/api/Avatar/get-by-email/{email}/{providerType}/{setGlobally}` | Get's the avatar for the given email. Only works for logged in users. Use Authe |
| GET | `/api/Avatar/get-by-email/{email}` | Get's the avatar for the given email. Only works for logged in users. Use Authe |
| GET | `/api/Avatar/get-by-id/{id}/{providerType}/{setGlobally}` | Get's the avatar for the given id. Only works for logged in users. Use Authenti |
| GET | `/api/Avatar/get-by-id/{id}` | Get's the avatar for the given id. Only works for logged in users. Use Authenti |
| GET | `/api/Avatar/get-by-username/{username}/{providerType}/{setGlobally}` | Get's the avatar for the given username. Only works for logged in users. Use Au |
| GET | `/api/Avatar/get-by-username/{username}` | Get's the avatar for the given username. Only works for logged in users. Use Au |
| GET | `/api/Avatar/get-logged-in-avatar/{providerType}/{setGlobally}` | Get's the logged in avatar. Only works for logged in users. Use Authenticate en |
| GET | `/api/Avatar/get-logged-in-avatar` | Get's the logged in avatar. Only works for logged in users. Use Authenticate en |
| GET | `/api/Avatar/get-terms` | Get's the terms & services agreement for creating an avatar and joining the OASI |
| GET | `/api/Avatar/get-uma-json-by-email/{email}/{providerType}/{setGlobally}` | Get's the 3D Model UMA JSON for a given avatar using their email. Only works fo |
| GET | `/api/Avatar/get-uma-json-by-email/{email}` | Get's the 3D Model UMA JSON for a given avatar using their email. Only works fo |
| GET | `/api/Avatar/get-uma-json-by-id/{id}/{providerType}/{setGlobally}` | Get's the 3D Model UMA JSON for a given avatar using their id. Only works for l |
| GET | `/api/Avatar/get-uma-json-by-id/{id}` | Get's the 3D Model UMA JSON for a given avatar using their id. Only works for l |
| GET | `/api/Avatar/get-uma-json-by-username/{username}/{providerType}/{setGlobally}` | Get's the 3D Model UMA JSON for a given avatar using their username. Only works |
| GET | `/api/Avatar/get-uma-json-by-username/{username}` | Get's the 3D Model UMA JSON for a given avatar using their username. Only works |
| POST | `/api/Avatar/refresh-token/{providerType}/{setGlobally}` | Refresh and generate a new JWT Security Token. This will only work if you are al |
| POST | `/api/Avatar/refresh-token` | Refresh and generate a new JWT Security Token. This will only work if you are al |
| POST | `/api/Avatar/register/{providerType}/{setGlobally}` | Register a new avatar. Pass in the provider you wish to use. Set the setglobally |
| POST | `/api/Avatar/register` | Register a new avatar with the OASIS system. |
| POST | `/api/Avatar/remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}` | Remove karma from the given avatar. karmaType = The type of negative karma, karm |
| POST | `/api/Avatar/remove-karma-from-avatar/{avatarId}` | Remove karma from the given avatar. karmaType = The type of negative karma, karm |
| POST | `/api/Avatar/reset-password/{providerType}/{setGlobally}` | Call this method passing in the reset token received in the forgotten password e |
| POST | `/api/Avatar/reset-password` | Call this method passing in the reset token received in the forgotten password e |
| POST | `/api/Avatar/revoke-token/{providerType}/{setGlobally}` | Revoke a given JWT Token (for example, if a user logs out). They must be logged  |
| POST | `/api/Avatar/revoke-token` | Revoke a given JWT Token (for example, if a user logs out).  Only works for log |
| POST | `/api/Avatar/search/{providerType}/{setGlobally}` | Search avatars for the given search term. Coming soon...  Pass in the provider  |
| POST | `/api/Avatar/search` | Search avatars for the given search term. Coming soon... |
| POST | `/api/Avatar/update-avatar-detail-by-email/{email}/{providerType}/{setGlobally}` | Update the given avatar detail with their avatar email address.  Only works for |
| POST | `/api/Avatar/update-avatar-detail-by-email/{email}` | Update the given avatar detail with their avatar email address.  Only works for |
| POST | `/api/Avatar/update-avatar-detail-by-id/{id}/{providerType}/{setGlobally}` | Update the given avatar detail by the avatar's id.  Only works for logged in us |
| POST | `/api/Avatar/update-avatar-detail-by-id/{id}` | Update the given avatar detail with their avatar id. Only works for logged in u |
| POST | `/api/Avatar/update-avatar-detail-by-username/{username}/{providerType}/{setGlobally}` | Update the given avatar detail with their avatar username.  Only works for logg |
| POST | `/api/Avatar/update-avatar-detail-by-username/{username}` | Update the given avatar detail with their avatar username.  Only works for logg |
| POST | `/api/Avatar/update-by-email/{email}/{providerType}/{setGlobally}` | Update the given avatar using their email address. Only works for logged in use |
| POST | `/api/Avatar/update-by-email/{email}` | Update the given avatar using their email address. Only works for logged in use |
| POST | `/api/Avatar/update-by-id/{id}/{providerType}/{setGlobally}` | Update the given avatar using their id. Only works for logged in users. Use Aut |
| POST | `/api/Avatar/update-by-id/{id}` | Update the given avatar using their id. Only works for logged in users. Use Aut |
| POST | `/api/Avatar/update-by-username/{username}/{providerType}/{setGlobally}` | Update the given avatar using their username. Only works for logged in users. U |
| POST | `/api/Avatar/update-by-username/{username}` | Update the given avatar using their username. Only works for logged in users. U |
| POST | `/api/Avatar/upload-avatar-portrait/{providerType}/{setGlobally}` | Upload's an avatar's portrait (2D Image), which is displayed on the web portal o |
| POST | `/api/Avatar/upload-avatar-portrait` | Upload's the avatar's portrait (2D Image), which is displayed on the web portal  |
| POST | `/api/Avatar/validate-reset-token/{providerType}/{setGlobally}` |  |
| POST | `/api/Avatar/validate-reset-token` |  |
| GET | `/api/Avatar/verify-email/{providerType}/{setGlobally}` | Verify a newly created avatar by passing in the validation token sent in the ver |
| POST | `/api/Avatar/verify-email/{providerType}/{setGlobally}` | Verify a newly created avatar by passing in the validation token sent in the ver |
| GET | `/api/Avatar/verify-email` | Verify a newly created avatar by passing in the validation token sent in the ver |
| POST | `/api/Avatar/verify-email` | Verify a newly created avatar by passing in the validation token sent in the ver |

## Bridge

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/v1/bridge/exchange-rate` | Gets the current exchange rate between two tokens. Optimized for low-latency re |
| GET | `/api/v1/bridge/networks` | Gets supported networks for bridge operations. |
| GET | `/api/v1/bridge/orders/{orderId}/check-balance` | Checks the balance and status of an existing bridge order. |
| POST | `/api/v1/bridge/orders/private` | Creates a private bridge order with viewing key audit and proof verification ena |
| POST | `/api/v1/bridge/orders` | Creates a new cross-chain bridge order (token swap). Executes atomic swap with  |
| POST | `/api/v1/bridge/proofs/verify` | Verifies a submitted zero-knowledge proof payload. |
| POST | `/api/v1/bridge/viewing-keys/audit` | Records a viewing key for auditability/compliance. |

## Chat

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/chat/history/{sessionId}` | Get chat session history |
| POST | `/api/chat/send-message/{sessionId}` | Send a message in a chat session |
| POST | `/api/chat/start-new-chat-session` | Starts a new chat session. PREVIEW - COMING SOON... |

## Data

| Method | Path | Summary |
|--------|------|---------|
| DELETE | `/api/data/delete-holon/{id}/{softDelete}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Delete a holon for the given id. Set SoftDelete to true if you wish this holon t |
| DELETE | `/api/data/delete-holon/{id}/{softDelete}/{providerType}/{setGlobally}` | Delete a holon for the given id. Set SoftDelete to true if you wish this holon t |
| DELETE | `/api/data/delete-holon/{id}/{softDelete}` | Delete a holon for the given id. Set SoftDelete to true if you wish this holon t |
| DELETE | `/api/data/delete-holon/{id}` | Delete a holon for the given id. |
| DELETE | `/api/data/delete-holon` | Delete a holon for the given id. Set SoftDelete to true if you wish this holon t |
| GET | `/api/data/load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Load's all holons for the given HolonType. Use 'All' to load all holons. Set th |
| GET | `/api/data/load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}` | Load's all holons for the given HolonType. Use 'All' to load all holons. Set th |
| GET | `/api/data/load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}` | Load's all holons for the given HolonType. Use 'All' to load all holons. Set th |
| GET | `/api/data/load-all-holons/{holonType}` | Load's all holons for the given HolonType. Use 'All' to load all holons. |
| POST | `/api/data/load-all-holons` | Load's all holons for the given HolonType. Use 'All' to load all holons. Set th |
| GET | `/api/data/load-data/{key}/{value}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Loads custom data with the given key from the current logged in avatar. Set the |
| GET | `/api/data/load-data/{key}/{value}/{providerType}/{setGlobally}` | Loads custom data with the given key from the current logged in avatar. Pass in |
| GET | `/api/data/load-data/{key}/{value}` | Loads custom data with the given key from the current logged in avatar. |
| POST | `/api/data/load-data` | Loads custom data with the given key from the current logged in avatar. |
| GET | `/api/data/load-file/{id}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Loads a file with the given id. Set the autoFailOverMode to 'ON' if you wish th |
| GET | `/api/data/load-file/{id}/{providerType}/{setGlobally}` | Loads a file with the given id. Pass in the provider you wish to use. Set the  |
| GET | `/api/data/load-file/{id}` | Loads a file with the given id. |
| POST | `/api/data/load-file` | Loads a file with the given id. |
| GET | `/api/data/load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Load's a holon data object for the given id. Set the loadChildren flag to true  |
| GET | `/api/data/load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}` | Load's a holon data object for the given id. Set the loadChildren flag to true  |
| GET | `/api/data/load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}` | Load's a holon data object for the given id. Set the loadChildren flag to true  |
| GET | `/api/data/load-holon/{id}` | Load's a holon data object for the given id. |
| POST | `/api/data/load-holon` | Load's a holon data object for the given id. Set the loadChildren flag to true  |
| GET | `/api/data/load-holons-for-parent/{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Load's all holons for the given parent and the given HolonType. Use 'All' to loa |
| GET | `/api/data/load-holons-for-parent/{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}` | Load's all holons for the given parent and the given HolonType. Use 'All' to loa |
| GET | `/api/data/load-holons-for-parent/{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}` | Load's all holons for the given parent and the given HolonType. Use 'All' to loa |
| GET | `/api/data/load-holons-for-parent/{id}/{holonType}` | Load's all holons for the given parent and the given HolonType. Use 'All' to loa |
| POST | `/api/data/load-holons-for-parent` | Load's all holons for the given parent and the given HolonType. Use 'All' to loa |
| GET | `/api/data/save-data/{key}/{value}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Saves custom data with a given key to the current logged in avatar. Set the aut |
| GET | `/api/data/save-data/{key}/{value}/{providerType}/{setGlobally}` | Saves custom data with a given key to the current logged in avatar. Pass in the |
| GET | `/api/data/save-data/{key}/{value}` | Saves custom data with a given key to the current logged in avatar. |
| POST | `/api/data/save-data` | Saves custom data with a given key to the current logged in avatar. |
| GET | `/api/data/save-file/{data}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Saves a file and returns the id linked to the holon that it is stored in. Set t |
| GET | `/api/data/save-file/{data}/{providerType}/{setGlobally}` | Saves a file and returns the id linked to the holon that it is stored in. Pass  |
| GET | `/api/data/save-file/{data}` | Saves a file and returns the id linked to the holon that it is stored in. |
| POST | `/api/data/save-file` | Saves a file and returns the id linked to the holon that it is stored in. |
| POST | `/api/data/save-holon-off-chain` | Save's a holon data object (meta data) to the given off-chain provider and then  |
| POST | `/api/data/save-holon/{holon}` | Save's a holon data object. |
| POST | `/api/data/save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{AutoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}` | Save's a holon data object. Set the saveChildren flag to true to save all the h |
| POST | `/api/data/save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{providerType}/{setGlobally}` | Save's a holon data object. Set the saveChildren flag to true to save all the h |
| POST | `/api/data/save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}` | Save's a holon data object. Set the saveChildren flag to true to save all the h |
| POST | `/api/data/save-holon` | Save's a holon data object. Set the saveChildren flag to true to save all the h |

## EOSIO

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/eosio/{avatarId}/{eosioAccountName}` | Link's a given eosioAccountName to the given avatar. |
| GET | `/api/eosio/get-avatar-for-eosio-account-name` | Get's the Avatar for the the given EOS account name. |
| GET | `/api/eosio/get-avatar-id-for-eosio-account-name` | Get's the Avatar id for the the given EOS account name. |
| GET | `/api/eosio/get-balance-for-avatar` | Get's the EOSIO balance for the given avatar. |
| GET | `/api/eosio/get-balance-for-eosio-account` | Get's the EOSIO balance for the given EOSIO account. |
| GET | `/api/eosio/get-eosio-account-for-avatar` | Get's the EOSIO account for the given Avatar. |
| GET | `/api/eosio/get-eosio-account-name-for-avatar` | Get's the EOSIO account name for the given Avatar. |
| GET | `/api/eosio/get-eosio-account-private-key-for-avatar` | Get's the EOSIO private key for the given Avatar. |
| GET | `/api/eosio/get-eosio-account` | Get's the EOSIO account. |

## Eggs

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/eggs/discover` | Discover a new egg |
| GET | `/api/eggs/get-all-eggs` | Get's all eggs currently hidden in the OASIS |
| GET | `/api/eggs/get-current-egg-quest-leader-board` | Get's the current egg quest leaderboard |
| GET | `/api/eggs/get-current-egg-quests` | Get's the current egg quests |
| POST | `/api/eggs/hatch/{eggId}` | Hatch an egg |
| GET | `/api/eggs/my-eggs` | Get eggs for the current avatar |

## Escrow

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/Escrow/{escrowId}/fund` | Fund escrow by transferring MNEE from payer |
| POST | `/api/Escrow/{escrowId}/release` | Release escrow funds to payee |
| GET | `/api/Escrow/{escrowId}` | Get escrow by ID |
| GET | `/api/Escrow/avatar/{avatarId}` | Get all escrows for an avatar |
| POST | `/api/Escrow/create` | Create a new escrow contract |

## Files

| Method | Path | Summary |
|--------|------|---------|
| DELETE | `/api/files/delete-file/{fileId}` | Delete a file by ID. |
| GET | `/api/files/download-file/{fileId}` | Download a file by ID. |
| GET | `/api/files/file-metadata/{fileId}` | Get file metadata by ID. |
| GET | `/api/files/get-all-files-stored-for-current-logged-in-avatar` | Get all files stored for the currently logged in avatar. |
| PUT | `/api/files/update-file-metadata/{fileId}` | Update file metadata. |
| POST | `/api/files/upload-file` | Upload a file for the current avatar. |
| POST | `/api/files/upload` | Upload a file to IPFS via Pinata (multipart/form-data). Returns the IPFS hash/U |

## Gifts

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/gifts/history` | Get gift history for the current avatar |
| GET | `/api/gifts/my-gifts` | Get all gifts for the current avatar |
| POST | `/api/gifts/open-gift/{giftId}` | Open a received gift |
| POST | `/api/gifts/receive-gift/{giftId}` | Receive a gift |
| POST | `/api/gifts/send-gift/{toAvatarId}` | Send a gift to another avatar |
| GET | `/api/gifts/stats` | Get gift statistics for the current avatar |

## Health

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/Health/health` | Health check endpoint for Railway deployment |
| GET | `/api/Health` | Health check endpoint for Railway deployment |

## Holochain

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/holochain/{avatarId}/{holochainAgentId}` | Link's a given holochain AgentId to the given avatar. |
| GET | `/api/holochain/get-avatar-for-holochain-agentid` | Get's the Avatar for the the given Holochain agent id. |
| GET | `/api/holochain/get-avatar-id-for-holochain-agentid` | Get's the Avatar id for the the given EOS account name. |
| GET | `/api/holochain/get-holo-fuel-balance-for-agentId` | Get's the HoloFuel balance for the given agent. |
| GET | `/api/holochain/get-holo-fuel-balance-for-avatar` | Get's the EOSIO balance for the given avatar. |
| GET | `/api/holochain/get-holochain-agent-private-keys-for-avatar` | Get's the Holochain Agent's private key's for the given Avatar. |
| GET | `/api/holochain/get-holochain-agentids-for-avatar` | Get's the Holochain Agent ID(s) for the given Avatar. |

## HyperDrive

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/HyperDrive/ai/recommendations` | Gets AI-powered optimization recommendations |
| POST | `/api/HyperDrive/ai/record-performance` | Records performance data for AI training |
| GET | `/api/HyperDrive/analytics/cost-optimization` | Gets cost optimization recommendations |
| GET | `/api/HyperDrive/analytics/performance-optimization` | Gets performance optimization recommendations |
| GET | `/api/HyperDrive/analytics/predictive/{providerType}` | Gets predictive analytics |
| POST | `/api/HyperDrive/analytics/record` | Records analytics data |
| GET | `/api/HyperDrive/analytics/report` | Gets comprehensive analytics report |
| GET | `/api/HyperDrive/best-provider` | Gets the best provider based on current strategy |
| POST | `/api/HyperDrive/config/reset` | Resets configuration to defaults |
| POST | `/api/HyperDrive/config/validate` | Validates the current configuration |
| GET | `/api/HyperDrive/config` | Gets the current HyperDrive configuration |
| PUT | `/api/HyperDrive/config` | Updates the HyperDrive configuration |
| GET | `/api/HyperDrive/connections` | Gets active connection counts for all providers |
| PUT | `/api/HyperDrive/cost/{providerType}` | Updates cost analysis for a provider |
| GET | `/api/HyperDrive/costs/current` |  |
| GET | `/api/HyperDrive/costs/history` |  |
| PUT | `/api/HyperDrive/costs/limits` |  |
| GET | `/api/HyperDrive/costs/projections` |  |
| GET | `/api/HyperDrive/dashboard` | Gets real-time dashboard data |
| GET | `/api/HyperDrive/data-permissions` | Gets data permissions configuration |
| PUT | `/api/HyperDrive/data-permissions` | Updates data permissions configuration |
| GET | `/api/HyperDrive/failover/escalation-rules` |  |
| PUT | `/api/HyperDrive/failover/escalation-rules` |  |
| GET | `/api/HyperDrive/failover/predictions` | Gets failure predictions |
| POST | `/api/HyperDrive/failover/preventive` | Initiates preventive failover |
| GET | `/api/HyperDrive/failover/provider-rules` |  |
| PUT | `/api/HyperDrive/failover/provider-rules` |  |
| POST | `/api/HyperDrive/failover/record-failure` | Records failure event |
| GET | `/api/HyperDrive/failover/rules` | Gets failover rules configuration |
| PUT | `/api/HyperDrive/failover/rules` | Updates failover rules configuration |
| DELETE | `/api/HyperDrive/failover/triggers/{id}` |  |
| PUT | `/api/HyperDrive/failover/triggers/{id}` |  |
| POST | `/api/HyperDrive/failover/triggers` |  |
| PUT | `/api/HyperDrive/geographic/{providerType}` | Updates geographic information for a provider |
| POST | `/api/HyperDrive/intelligent-mode/disable` | Disables intelligent mode |
| POST | `/api/HyperDrive/intelligent-mode/enable` | Enables intelligent mode |
| GET | `/api/HyperDrive/intelligent-mode` | Gets intelligent mode configuration |
| PUT | `/api/HyperDrive/intelligent-mode` | Updates intelligent mode configuration |
| POST | `/api/HyperDrive/metrics/{providerType}/reset` | Resets metrics for a specific provider |
| GET | `/api/HyperDrive/metrics/{providerType}` | Gets performance metrics for a specific provider |
| POST | `/api/HyperDrive/metrics/reset-all` | Resets all metrics |
| GET | `/api/HyperDrive/metrics` | Gets performance metrics for all providers |
| GET | `/api/HyperDrive/mode` | Gets/sets HyperDrive mode (Legacy \| OASISHyperDrive2) |
| PUT | `/api/HyperDrive/mode` |  |
| GET | `/api/HyperDrive/providers/free` | Gets free providers list |
| GET | `/api/HyperDrive/providers/low-cost` | Gets low-cost providers list |
| GET | `/api/HyperDrive/quota/limits` | Gets quota limits for current subscription |
| GET | `/api/HyperDrive/quota/status` | Checks quota status for a specific type |
| GET | `/api/HyperDrive/quota/usage` | Gets current usage statistics |
| GET | `/api/HyperDrive/recommendations/security` |  |
| GET | `/api/HyperDrive/recommendations/smart` |  |
| POST | `/api/HyperDrive/record-connection` | Records connection activity |
| POST | `/api/HyperDrive/record-request` | Records a request for performance tracking |
| GET | `/api/HyperDrive/replication/cost-optimization` |  |
| PUT | `/api/HyperDrive/replication/cost-optimization` |  |
| GET | `/api/HyperDrive/replication/data-type-rules` |  |
| PUT | `/api/HyperDrive/replication/data-type-rules` |  |
| GET | `/api/HyperDrive/replication/provider-rules` |  |
| PUT | `/api/HyperDrive/replication/provider-rules` |  |
| GET | `/api/HyperDrive/replication/rules` | Gets replication rules configuration |
| PUT | `/api/HyperDrive/replication/rules` | Updates replication rules configuration |
| GET | `/api/HyperDrive/replication/schedule-rules` |  |
| PUT | `/api/HyperDrive/replication/schedule-rules` |  |
| DELETE | `/api/HyperDrive/replication/triggers/{id}` |  |
| PUT | `/api/HyperDrive/replication/triggers/{id}` |  |
| POST | `/api/HyperDrive/replication/triggers` |  |
| GET | `/api/HyperDrive/status` | Gets HyperDrive status and health |
| GET | `/api/HyperDrive/subscription/config` | Gets subscription configuration |
| PUT | `/api/HyperDrive/subscription/config` | Updates subscription configuration |
| DELETE | `/api/HyperDrive/subscription/quota-notifications/{id}` |  |
| PUT | `/api/HyperDrive/subscription/quota-notifications/{id}` |  |
| GET | `/api/HyperDrive/subscription/quota-notifications` |  |
| POST | `/api/HyperDrive/subscription/quota-notifications` |  |
| DELETE | `/api/HyperDrive/subscription/usage-alerts/{id}` |  |
| PUT | `/api/HyperDrive/subscription/usage-alerts/{id}` |  |
| GET | `/api/HyperDrive/subscription/usage-alerts` |  |
| POST | `/api/HyperDrive/subscription/usage-alerts` |  |

## Invoice

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/Invoice/{invoiceId}/cancel` | Cancel an invoice |
| POST | `/api/Invoice/{invoiceId}/pay` | Pay an invoice using MNEE |
| GET | `/api/Invoice/{invoiceId}` | Get invoice by ID |
| GET | `/api/Invoice/avatar/{avatarId}` | Get all invoices for an avatar |
| POST | `/api/Invoice/create` | Create a new invoice |

## Karma

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/karma/add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}` | Add positive karma to the given avatar. karmaType = The type of positive karma,  |
| POST | `/api/karma/add-karma-to-avatar/{avatarId}` | Add positive karma to the given avatar. karmaType = The type of positive karma,  |
| GET | `/api/karma/get-karma-akashic-records-for-avatar/{avatarId}/{providerType}/{setGlobally}` | Get's the karma akashic records for a given avatar. Pass in the provider you wis |
| GET | `/api/karma/get-karma-akashic-records-for-avatar/{avatarId}` | Get's the karma akashic records for a given avatar. |
| GET | `/api/karma/get-karma-for-avatar/{avatarId}/{providerType}/{setGlobally}` | Get's the karma for a given avatar. Pass in the provider you wish to use. Set th |
| GET | `/api/karma/get-karma-for-avatar/{avatarId}` | Get's the karma for a given avatar. |
| GET | `/api/karma/get-karma-history/{avatarId}/{providerType}/{setGlobally}` | Gets karma history (paged) for a given avatar with provider activation. |
| GET | `/api/karma/get-karma-history/{avatarId}` | Gets karma history (paged) for a given avatar. |
| GET | `/api/karma/get-karma-stats/{avatarId}/{providerType}/{setGlobally}` | Gets karma statistics for a given avatar with provider activation. |
| GET | `/api/karma/get-karma-stats/{avatarId}` | Gets karma statistics (totals, distributions, recent activity) for a given avata |
| GET | `/api/karma/get-negative-karma-weighting/{karmaType}/{providerType}/{setGlobally}` | Get karma weighting for a given negative karma cateogey. |
| GET | `/api/karma/get-negative-karma-weighting/{karmaType}` | Get karma weighting for a given negative karma category. |
| GET | `/api/karma/get-positive-karma-weighting/{karmaType}/{providerType}/{setGlobally}` | Get karma weighting for a given positive karma category with specific provider. |
| GET | `/api/karma/get-positive-karma-weighting/{karmaType}` | Get karma weighting for a given positive karma category. |
| POST | `/api/karma/remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}` | Remove karma from the given avatar. They must be logged in & authenticated for t |
| POST | `/api/karma/remove-karma-from-avatar/{avatarId}` | Remove karma from the given avatar. They must be logged in & authenticated for t |
| POST | `/api/karma/set-negative-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}` | Set's the weighting for a given positive karma category. Pass in the provider yo |
| POST | `/api/karma/set-negative-karma-weighting/{karmaType}/{weighting}` | Set's the weighting for a given negative karma category. |
| POST | `/api/karma/set-positive-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}` | Set's the weighting for a given positive karma category. Pass in the provider yo |
| POST | `/api/karma/set-positive-karma-weighting/{karmaType}/{weighting}` | Set's the weighting for a given positive karma category. |
| POST | `/api/karma/vote-for-negative-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}` | Allows people to vote what they feel should be the weighting for a given negativ |
| POST | `/api/karma/vote-for-negative-karma-weighting/{karmaType}/{weighting}` | Allows people to vote what they feel should be the weighting for a given negativ |
| POST | `/api/karma/vote-for-positive-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}` | Allows people to vote what they feel should be the weighting for a given positiv |
| POST | `/api/karma/vote-for-positive-karma-weighting/{karmaType}/{weighting}` | Allows people to vote what they feel should be the weighting for a given positiv |

## Keys

| Method | Path | Summary |
|--------|------|---------|
| DELETE | `/api/Keys/{keyId}` | Deletes a key |
| PUT | `/api/Keys/{keyId}` | Updates an existing key |
| GET | `/api/Keys/all` | Gets all keys for the authenticated avatar |
| POST | `/api/Keys/base58_check_decode/{data}` | Decodes. |
| POST | `/api/Keys/clear_cache` | Clear's the KeyManager's internal cache of keys. |
| POST | `/api/Keys/create` | Creates a new key for the authenticated avatar |
| POST | `/api/Keys/decode_private_wif/{data}` | Decode's the private WIF. |
| POST | `/api/Keys/encode_signature/{source}` | Encode's the signature. |
| POST | `/api/Keys/generate_keypair_and_link_provider_keys_to_avatar_by_email` | Generate's a new unique private/public keypair & then links to the given avatar  |
| POST | `/api/Keys/generate_keypair_and_link_provider_keys_to_avatar_by_id` | Generate's a new unique private/public keypair & then links to the given avatar  |
| POST | `/api/Keys/generate_keypair_and_link_provider_keys_to_avatar_by_username` | Generate's a new unique private/public keypair & then links to the given avatar  |
| POST | `/api/Keys/generate_keypair_for_provider/{providerType}` | Generate's a new unique private/public keypair for a given provider type. |
| POST | `/api/Keys/generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_email` | Generate's a new unique private/public keypair with wallet address & then links  |
| POST | `/api/Keys/generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_id` | Generate's a new unique private/public keypair with wallet address & then links  |
| POST | `/api/Keys/generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_username` | Generate's a new unique private/public keypair with wallet address & then links  |
| POST | `/api/Keys/generate_keypair_with_wallet_address_for_provider/{providerType}` | Generate's a new unique private/public keypair with wallet address for a given p |
| GET | `/api/Keys/get_all_provider_private_keys_for_avatar_by_id/{id}` | Get's a given avatar's private keys for the given avatar with their id. |
| GET | `/api/Keys/get_all_provider_private_keys_for_avatar_by_username/{username}` | Get's a given avatar's private keys for the given avatar with their username. |
| GET | `/api/Keys/get_all_provider_public_keys_for_avatar_by_email/{email}` | Get's a given avatar's public keys for the given avatar with their email. |
| GET | `/api/Keys/get_all_provider_public_keys_for_avatar_by_id/{id}` | Get's a given avatar's public keys for the given avatar with their id. |
| GET | `/api/Keys/get_all_provider_public_keys_for_avatar_by_username/{username}` | Get's a given avatar's public keys for the given avatar with their username. |
| GET | `/api/Keys/get_all_provider_unique_storage_keys_for_avatar_by_email/{email}` | Get's a given avatar's unique storage keys for the given avatar with their email |
| GET | `/api/Keys/get_all_provider_unique_storage_keys_for_avatar_by_id/{id}` | Get's a given avatar's unique storage keys for the given avatar with their id. |
| GET | `/api/Keys/get_all_provider_unique_storage_keys_for_avatar_by_username/{username}` | Get's a given avatar's unique storage keys for the given avatar with their usern |
| GET | `/api/Keys/get_avatar_email_for_provider_public_key/{providerKey}` | Get's the avatar email for a given public key. |
| GET | `/api/Keys/get_avatar_email_for_provider_unique_storage_key/{providerKey}` | Get's the avatar email for a given unique storage key. |
| GET | `/api/Keys/get_avatar_for_provider_public_key/{providerKey}` | Get's the avatar for a given public key. |
| GET | `/api/Keys/get_avatar_for_provider_unique_storage_key/{providerKey}` | Get's the avatar for a given unique storage key. |
| GET | `/api/Keys/get_avatar_id_for_provider_public_key/{providerKey}` | Get's the avatar id for a given public key. |
| GET | `/api/Keys/get_avatar_id_for_provider_unique_storage_key/{providerKey}` | Get's the avatar id for a given unique storage key. |
| GET | `/api/Keys/get_avatar_username_for_provider_public_key/{providerKey}` | Get's the avatar username for a given public key. |
| GET | `/api/Keys/get_avatar_username_for_provider_unique_storage_key/{providerKey}` | Get's the avatar username for a given unique storage key. |
| POST | `/api/Keys/get_private_wifi/{source}` | Get's the private WIF. |
| GET | `/api/Keys/get_provider_private_key_for_avatar_by_id` | Get's a given avatar's private key for the given provider type using the avatar' |
| GET | `/api/Keys/get_provider_private_key_for_avatar_by_username` | Get's a given avatar's private key for the given provider type using the avatar' |
| GET | `/api/Keys/get_provider_public_keys_for_avatar_by_email` | Get's a given avatar's public keys for the given provider type using the avatar' |
| GET | `/api/Keys/get_provider_public_keys_for_avatar_by_id` | Get's a given avatar's public keys for the given provider type using the avatar' |
| GET | `/api/Keys/get_provider_public_keys_for_avatar_by_username` | Get's a given avatar's public keys for the given provider type using the avatar' |
| GET | `/api/Keys/get_provider_unique_storage_key_for_avatar_by_email` | Get's a given avatar's unique storage key for the given provider type using the  |
| GET | `/api/Keys/get_provider_unique_storage_key_for_avatar_by_id` | Get's a given avatar's unique storage key for the given provider type using the  |
| GET | `/api/Keys/get_provider_unique_storage_key_for_avatar_by_username` | Get's a given avatar's unique storage key for the given provider type using the  |
| POST | `/api/Keys/get_public_wifi` | Get's the public WIF. |
| POST | `/api/Keys/link_provider_private_key_to_avatar_by_id` | Link's a given Avatar to a Providers Private Key (password, crypto private key,  |
| POST | `/api/Keys/link_provider_private_key_to_avatar_by_username` | Link's a given Avatar to a Providers Private Key (password, crypto private key,  |
| POST | `/api/Keys/link_provider_public_key_to_avatar_by_email` | Link's a given Avatar to a Providers Public Key (private/public key pairs or use |
| POST | `/api/Keys/link_provider_public_key_to_avatar_by_id` | Link's a given Avatar to a Providers Public Key (private/public key pairs or use |
| POST | `/api/Keys/link_provider_public_key_to_avatar_by_username` | Link's a given Avatar to a Providers Public Key (private/public key pairs or use |
| POST | `/api/Keys/link_provider_wallet_address_to_avatar_by_email` | Link's a given Avatar to a Provider's Wallet Address by email. |
| POST | `/api/Keys/link_provider_wallet_address_to_avatar_by_id` | Link's a given Avatar to a Provider's Wallet Address by avatar ID. |
| POST | `/api/Keys/link_provider_wallet_address_to_avatar_by_username` | Link's a given Avatar to a Provider's Wallet Address by username. |
| GET | `/api/Keys/stats` | Gets key statistics for the authenticated avatar |

## MNEE

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/MNEE/allowance/{avatarId}` |  |
| POST | `/api/MNEE/approve` | Approve MNEE spending for a spender address (convenience wrapper) Uses MNEE con |
| GET | `/api/MNEE/balance/{avatarId}` | Get MNEE balance for an avatar (convenience wrapper) Uses MNEE contract address |
| GET | `/api/MNEE/token-info` | Get MNEE token information (convenience wrapper) Uses MNEE contract address: 0x |
| POST | `/api/MNEE/transfer` | Transfer MNEE between avatars or to external address (convenience wrapper) Uses |

## Map

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/map/CreateAndDrawRouteOnMapBetweenHolons/{holonDNA}` | Create and draw a route on the map between two holons. |
| POST | `/api/map/CreateAndDrawRouteOnMapBeweenPoints/{points}` | Create and draw a route on the map between two points. |
| POST | `/api/map/Draw2DSpriteOnHUD/{sprite}/{x}/{y}` | Draw a 2D sprint on the Our World HUD. |
| POST | `/api/map/Draw2DSpriteOnMap/{sprite}/{x}/{y}` | Draw a 2D sprint on the map. |
| POST | `/api/map/Draw3DObjectOnMap/{obj}/{x}/{y}` | Draw a 3D object on the map. |
| POST | `/api/map/HighlightBuildingOnMap/{building}` | Highlight a building on the map. |
| GET | `/api/map/nearby` | Get nearby locations for the current avatar |
| POST | `/api/map/PamMapDown/{value}` | Pam the map down. |
| POST | `/api/map/PamMapLeft/{value}` | Pam the map left. |
| POST | `/api/map/PamMapRight/{value}` | Pam the map right. |
| POST | `/api/map/PamMapUp/{value}` | Pam the map up. |
| GET | `/api/map/search-locations` | Search for locations |
| POST | `/api/map/search` | Search the map for locations, points of interest, and other map features |
| POST | `/api/map/SelectBuildingOnMap/{building}` | Select building on map. |
| POST | `/api/map/SelectHolonOnMap/{holon}` | Select holon on map. |
| POST | `/api/map/SelectQuestOnMap/{quest}` | Select quest on map. |
| GET | `/api/map/stats` | Get map statistics for the current avatar |
| GET | `/api/map/visit-history` | Get visit history for the current avatar |
| POST | `/api/map/visit/{locationId}` | Visit a location |
| POST | `/api/map/ZoomMapIn/{value}` | Zoom map in. |
| POST | `/api/map/ZoomMapOut/{value}` | Zoom map out. |
| POST | `/api/map/ZoomToHolonOnMap/{holon}` | Zoom to holon on map. |
| POST | `/api/map/ZoomToQuestOnMap/{quest}` | Zoom to quest on map. |

## Messaging

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/messaging/conversation/{otherAvatarId}` | Get conversation between current avatar and another avatar |
| POST | `/api/messaging/mark-messages-read` | Mark messages as read |
| POST | `/api/messaging/mark-notifications-read` | Mark notifications as read |
| GET | `/api/messaging/messages` | Get messages for the current avatar |
| GET | `/api/messaging/notifications` | Get notifications for the current avatar |
| POST | `/api/messaging/send-message-to-avatar/{toAvatarId}` | Send's a message to the given avatar |

## Nft

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/Nft/create-web4-nft-collection` | Creates a new Web4 NFT collection. |
| POST | `/api/Nft/export-web4-nft-to-file/{oasisNFTId}/{fullPathToExportTo}` | Exports a Web4 NFT to a JSON file. |
| POST | `/api/Nft/export-web4-nft` | Exports a Web4 NFT object. |
| GET | `/api/Nft/get-nft-provider-from-provider-type/{providerType}` |  |
| POST | `/api/Nft/import-web3-nft` | Imports a Web3 NFT into the OASIS system. |
| POST | `/api/Nft/import-web4-nft-from-file/{importedByAvatarId}/{fullPathToOASISNFTJsonFile}` | Imports a Web4 NFT from a JSON file. |
| POST | `/api/Nft/import-web4-nft/{importedByAvatarId}` | Imports a Web4 NFT object. |
| GET | `/api/Nft/load-all-geo-nfts-for-avatar/{avatarId}/{providerType}/{setGlobally}` |  |
| GET | `/api/Nft/load-all-geo-nfts-for-avatar/{avatarId}` |  |
| GET | `/api/Nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}` |  |
| GET | `/api/Nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}` |  |
| GET | `/api/Nft/load-all-geo-nfts/{providerType}/{setGlobally}` |  |
| GET | `/api/Nft/load-all-geo-nfts` |  |
| GET | `/api/Nft/load-all-nfts-for_avatar/{avatarId}/{providerType}/{setGlobally}` |  |
| GET | `/api/Nft/load-all-nfts-for_avatar/{avatarId}` |  |
| GET | `/api/Nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}` |  |
| GET | `/api/Nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress}` |  |
| GET | `/api/Nft/load-all-nfts/{providerType}/{setGlobally}` |  |
| GET | `/api/Nft/load-all-nfts` |  |
| GET | `/api/Nft/load-all-web3-nfts-for-avatar/{avatarId}` | Loads all Web3 NFTs for a specific avatar. |
| GET | `/api/Nft/load-all-web3-nfts-for-mint-address/{mintWalletAddress}` | Loads all Web3 NFTs for a specific mint wallet address. |
| GET | `/api/Nft/load-all-web3-nfts` | Loads all Web3 NFTs (admin only). |
| GET | `/api/Nft/load-nft-by-hash/{hash}/{providerType}/{setGlobally}` |  |
| GET | `/api/Nft/load-nft-by-hash/{hash}` |  |
| GET | `/api/Nft/load-nft-by-id/{id}/{providerType}/{setGlobally}` |  |
| GET | `/api/Nft/load-nft-by-id/{id}` | Loads an NFT by its unique identifier. |
| GET | `/api/Nft/load-web3-nft-by-hash/{onChainNftHash}` | Loads a Web3 NFT by its on-chain hash. |
| GET | `/api/Nft/load-web3-nft-by-id/{id}` | Loads a Web3 NFT by its unique identifier. |
| POST | `/api/Nft/mint-and-place-geo-nft` |  |
| POST | `/api/Nft/mint-nft` |  |
| POST | `/api/Nft/place-geo-nft` |  |
| POST | `/api/Nft/remint-nft` | Remints an existing NFT. |
| GET | `/api/Nft/search-web4-geo-nfts/{searchTerm}/{avatarId}` | Searches for Web4 Geo NFTs. |
| GET | `/api/Nft/search-web4-nft-collections/{searchTerm}/{avatarId}` | Searches for Web4 NFT collections. |
| GET | `/api/Nft/search-web4-nfts/{searchTerm}/{avatarId}` | Searches for Web4 NFTs. |
| POST | `/api/Nft/send-nft` |  |

## OLand

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/OLand/delete-oland/{olandId}` |  |
| GET | `/api/OLand/get-oland-price` |  |
| GET | `/api/OLand/load-all-olands` |  |
| GET | `/api/OLand/load-oland/{olandId}` |  |
| POST | `/api/OLand/purchase-oland` |  |
| POST | `/api/OLand/save-oland` |  |
| POST | `/api/OLand/update-oland` |  |

## OLandUnit

| Method | Path | Summary |
|--------|------|---------|
| DELETE | `/api/admin/OLandUnit/{id}` |  |
| GET | `/api/admin/OLandUnit/{id}` |  |
| PUT | `/api/admin/OLandUnit/{id}` |  |
| GET | `/api/admin/OLandUnit/GetAll` |  |
| POST | `/api/admin/OLandUnit` |  |

## ONET

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/v1/onet/network/broadcast` | Broadcast message to network |
| POST | `/api/v1/onet/network/connect` | Connect to a specific node |
| POST | `/api/v1/onet/network/disconnect` | Disconnect from a specific node |
| GET | `/api/v1/onet/network/nodes` | Get connected nodes |
| POST | `/api/v1/onet/network/start` | Start P2P network |
| GET | `/api/v1/onet/network/stats` | Get network statistics |
| GET | `/api/v1/onet/network/status` | Get P2P network status |
| POST | `/api/v1/onet/network/stop` | Stop P2P network |
| GET | `/api/v1/onet/network/topology` | Get network topology |
| GET | `/api/v1/onet/oasisdna` | Get OASISDNA configuration for ONET |
| PUT | `/api/v1/onet/oasisdna` | Update OASISDNA configuration for ONET |

## ONODE

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/v1/onode/config` | Get node configuration |
| PUT | `/api/v1/onode/config` | Update node configuration |
| GET | `/api/v1/onode/info` | Get node information |
| GET | `/api/v1/onode/logs` | Get node logs |
| GET | `/api/v1/onode/metrics` | Get node performance metrics |
| GET | `/api/v1/onode/oasisdna` | Get OASISDNA configuration for ONODE |
| PUT | `/api/v1/onode/oasisdna` | Update OASISDNA configuration for ONODE |
| GET | `/api/v1/onode/peers` | Get connected peers |
| POST | `/api/v1/onode/restart` | Restart ONODE |
| POST | `/api/v1/onode/start` | Start ONODE |
| GET | `/api/v1/onode/stats` | Get node statistics |
| GET | `/api/v1/onode/status` | Get node status |
| POST | `/api/v1/onode/stop` | Stop ONODE |

## Provider

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/provider/activate-provider/{providerType}` | Activate the given provider. |
| POST | `/api/provider/deactivate-provider/{providerType}` | Deactivate the given provider. |
| GET | `/api/provider/get-all-registered-network-providers` | Get all registered network providers. |
| GET | `/api/provider/get-all-registered-provider-types` | Get all registered provider types. |
| GET | `/api/provider/get-all-registered-providers-for-category/{category}` | Get all registered storage providers for a given category. |
| GET | `/api/provider/get-all-registered-providers` | Get all registered providers. |
| GET | `/api/provider/get-all-registered-renderer-providers` | Get all registered renderer providers. |
| GET | `/api/provider/get-all-registered-storage-providers` | Get all registered storage providers. |
| GET | `/api/provider/get-current-storage-provider-type` | Get's the current active storage provider type. |
| GET | `/api/provider/get-current-storage-provider` | Get's the current active storage provider. |
| GET | `/api/provider/get-providers-that-are-auto-replicating` | Get's the list of providers that are auto-replicating. See SetAutoReplicate meth |
| GET | `/api/provider/get-providers-that-have-auto-fail-over-enabled` | Get's the list of providers that have auto-failover enabled. See SetAutoFailOver |
| GET | `/api/provider/get-providers-that-have-auto-load-balance-enabled` | Get's the list of providers that have auto-load balance enabled. See SetAutoLoad |
| GET | `/api/provider/get-registered-provider/{providerType}` | Get's the provider for the given providerType. (The provider must already be reg |
| GET | `/api/provider/is-provider-registered/{providerType}` | Return true if the given provider has been registered, false if it has not. |
| POST | `/api/provider/register-provider-type/{providerType}` | Register the given provider type. |
| POST | `/api/provider/register-provider-types/{providerTypes}` | Register the given provider types. |
| POST | `/api/provider/register-provider/{provider}` | Register the given provider. |
| POST | `/api/provider/register-providers/{providers}` | Register the given providers. |
| POST | `/api/provider/set-and-activate-current-storage-provider/{providerType}/{setGlobally}` | Set and activate the current storage provider. If the setGlobally flag is false  |
| POST | `/api/provider/set-auto-fail-over-for-all-providers/{addToFailOverList}` | Enable/disable auto-failover for all providers. If this is set to true then the  |
| POST | `/api/provider/set-auto-fail-over-for-list-of-providers/{addToFailOverList}/{providerTypes}` | Enable/disable auto-failover for providers. If this is set to true then the OASI |
| POST | `/api/provider/set-auto-load-balance-for-all-providers/{addToLoadBalanceList}` | Enable/disable auto-load balance for all providers. If this is set to true then  |
| POST | `/api/provider/set-auto-load-balance-for-list-of-providers/{addToLoadBalanceList}/{providerTypes}` | Enable/disable auto-load balance for providers. If this is set to true then the  |
| POST | `/api/provider/set-auto-replicate-for-all-providers/{autoReplicate}` | Enable/disable auto-replication between providers. If this is set to true then t |
| POST | `/api/provider/set-auto-replicate-for-list-of-providers/{autoReplicate}/{providerTypes}` | Enable/disable auto-replication between providers. If this is set to true then t |
| POST | `/api/provider/set-provider-config/{providerType}/{connectionString}` | Override a provider's config such as connnectionstring, etc |
| POST | `/api/provider/unregister-provider-type/{providerType}` | Unregister the given provider. |
| POST | `/api/provider/unregister-provider-types/{providerTypes}` | Unregisters the list of providers passed in. |
| POST | `/api/provider/unregister-provider/{provider}` | Unregister the given provider. |
| POST | `/api/provider/unregister-providers/{providers}` | Unregisters the list of providers passed in. |

## Search

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/Search/{searchParams}/{providerType}/{setGlobally}` |  |
| GET | `/api/Search/{searchParams}` | Performs a search across all OASIS data using the specified search parameters. |

## Seeds

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/seeds/avatar/{avatarId}/transactions` | Gets all seed transactions for the specified avatar. |
| GET | `/api/seeds/me/transactions` | Gets all seed transactions for the current avatar. |
| POST | `/api/seeds/save-seed-transaction` | Saves a seed transaction for an avatar. |

## Settings

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/settings/get-all-settings-for-current-logged-in-avatar` | Get all OASIS settings for the currently logged in avatar |
| GET | `/api/settings/hyperdrive-settings` | Get HyperDrive settings for the current avatar |
| PUT | `/api/settings/hyperdrive-settings` | Update HyperDrive settings |
| GET | `/api/settings/notification-preferences` | Get notification preferences |
| PUT | `/api/settings/notification-preferences` | Update notification preferences |
| GET | `/api/settings/privacy-settings` | Get privacy settings |
| PUT | `/api/settings/privacy-settings` | Update privacy settings |
| GET | `/api/settings/subscription-settings` | Get subscription settings for the current avatar |
| PUT | `/api/settings/subscription-settings` | Update subscription settings |
| GET | `/api/settings/system-config` | Get system configuration |
| GET | `/api/settings/system-settings` | Get system settings for the current avatar |
| PUT | `/api/settings/system-settings` | Update system settings |
| PUT | `/api/settings/update-settings` | Update avatar settings |
| GET | `/api/settings/version` | Returns the current OASIS API Version |

## Share

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/share/share-holon/{holonId}/{avatarId}` | Share a given holon with a given avatar. PREVIEW - COMING SOON... |
| GET | `/api/share/share-holon/{holonId}/{avatarIds}` | Share a given holon with a groups of avatars. PREVIEW - COMING SOON... |

## Social

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/social/register-social-provider` | Register a given social provider (FaceBook, Twitter, Instagram, LinkedIn, etc) |
| GET | `/api/social/registered-providers` | Get registered social providers for the current avatar |
| POST | `/api/social/share-holon` | Share a holon to social media |
| GET | `/api/social/social-feed` | Get's the social feed from all registered social providers for the currently log |

## Solana

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/Solana/Mint` | Mint NFT (non-fungible token) |
| POST | `/api/Solana/Send` | Handles a transaction between accounts with a specific Lampposts size |

## Stats

| Method | Path | Summary |
|--------|------|---------|
| GET | `/api/stats/chat-stats/{avatarId}` | Get chat statistics for an avatar |
| GET | `/api/stats/get-stats-for-current-logged-in-avatar` | Get comprehensive stats for the currently logged in avatar |
| GET | `/api/stats/gift-stats/{avatarId}` | Get gift statistics for an avatar |
| GET | `/api/stats/karma-history/{avatarId}` | Get karma history for an avatar |
| GET | `/api/stats/karma-stats/{avatarId}` | Get karma statistics for an avatar |
| GET | `/api/stats/key-stats/{avatarId}` | Get key statistics for an avatar |
| GET | `/api/stats/leaderboard-stats/{avatarId}` | Get leaderboard statistics for an avatar |
| GET | `/api/stats/system-stats` | Get system-wide statistics |

## Subscription

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/Subscription/check-hyperdrive-quota` | Checks if user can perform HyperDrive operation based on quota |
| POST | `/api/Subscription/checkout/session` |  |
| GET | `/api/Subscription/hyperdrive-usage` | Gets HyperDrive usage statistics for current subscription |
| GET | `/api/Subscription/orders/me` |  |
| GET | `/api/Subscription/plans` |  |
| GET | `/api/Subscription/subscriptions/me` |  |
| POST | `/api/Subscription/toggle-pay-as-you-go` |  |
| POST | `/api/Subscription/update-hyperdrive-config` | Updates HyperDrive configuration based on subscription plan |
| GET | `/api/Subscription/usage` |  |
| POST | `/api/Subscription/webhooks/stripe` |  |

## Treasury

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/Treasury/{treasuryId}/allocate` | Execute automated fund allocation workflow |
| GET | `/api/Treasury/{treasuryId}/balance` | Get treasury balance summary |
| GET | `/api/Treasury/{treasuryId}` | Get treasury by ID |
| GET | `/api/Treasury/avatar/{avatarId}` | Get all treasuries for an avatar |
| POST | `/api/Treasury/create` | Create a new treasury |

## Video

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/video/end-call/{callId}` | End a video call |
| POST | `/api/video/generate` | Generate video using LTX.io API (text-to-video or image-to-video) Wraps the MCP |
| POST | `/api/video/join-call/{callId}` | Join an existing video call |
| POST | `/api/video/start-video-call` | Start's a video call. PREVIEW - COMING SOON... |

## Wallet

| Method | Path | Summary |
|--------|------|---------|
| POST | `/api/Wallet/avatar/{avatarId}/create-wallet` | Create a new wallet for an avatar by ID. |
| POST | `/api/Wallet/avatar/{avatarId}/import/private-key` | Import a wallet using private key by avatar ID. |
| POST | `/api/Wallet/avatar/{avatarId}/import/public-key` | Import a wallet using public key by avatar ID. |
| GET | `/api/Wallet/avatar/{avatarId}/portfolio/value` | Get total portfolio value across all wallets for an avatar. |
| GET | `/api/Wallet/avatar/{avatarId}/wallet/{walletId}/analytics` | Get wallet analytics for an avatar. |
| GET | `/api/Wallet/avatar/{avatarId}/wallet/{walletId}/tokens` | Get wallet tokens for an avatar. |
| PUT | `/api/Wallet/avatar/{avatarId}/wallet/{walletId}` | Update a wallet for an avatar by ID. |
| GET | `/api/Wallet/avatar/{avatarId}/wallets/chain/{chain}` | Get wallets by chain for an avatar. |
| POST | `/api/Wallet/avatar/{id}/default-wallet/{walletId}` | Set the default wallet for an avatar by ID. |
| GET | `/api/Wallet/avatar/{id}/default-wallet` | Get the default wallet for an avatar by ID. |
| GET | `/api/Wallet/avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}` | Load all provider wallets for an avatar by ID. |
| POST | `/api/Wallet/avatar/{id}/wallets` | Save provider wallets for an avatar by ID. |
| POST | `/api/Wallet/avatar/email/{email}/create-wallet` | Create a new wallet for an avatar by email. |
| POST | `/api/Wallet/avatar/email/{email}/default-wallet/{walletId}` | Set the default wallet for an avatar by email. |
| GET | `/api/Wallet/avatar/email/{email}/default-wallet` | Get the default wallet for an avatar by email. |
| POST | `/api/Wallet/avatar/email/{email}/import/private-key` | Import a wallet using private key by email. |
| POST | `/api/Wallet/avatar/email/{email}/import/public-key` | Import a wallet using public key by email. |
| PUT | `/api/Wallet/avatar/email/{email}/wallet/{walletId}` | Update a wallet for an avatar by email. |
| GET | `/api/Wallet/avatar/email/{email}/wallets/{showOnlyDefault}/{decryptPrivateKeys}/{providerType}` | Load all provider wallets for an avatar by email. |
| GET | `/api/Wallet/avatar/email/{email}/wallets` | Load all provider wallets for an avatar by email. |
| POST | `/api/Wallet/avatar/email/{email}/wallets` | Save provider wallets for an avatar by email. |
| POST | `/api/Wallet/avatar/username/{username}/create-wallet` | Create a new wallet for an avatar by username. |
| GET | `/api/Wallet/avatar/username/{username}/default-wallet/{showOnlyDefault}/{decryptPrivateKeys}` | Get the default wallet for an avatar by username. |
| POST | `/api/Wallet/avatar/username/{username}/default-wallet/{walletId}` | Set the default wallet for an avatar by username. |
| POST | `/api/Wallet/avatar/username/{username}/import/private-key` | Import a wallet using private key by username. |
| POST | `/api/Wallet/avatar/username/{username}/import/public-key` | Import a wallet using public key by username. |
| PUT | `/api/Wallet/avatar/username/{username}/wallet/{walletId}` | Update a wallet for an avatar by username. |
| GET | `/api/Wallet/avatar/username/{username}/wallets/{showOnlyDefault}/{decryptPrivateKeys}` | Load all provider wallets for an avatar by username. |
| POST | `/api/Wallet/avatar/username/{username}/wallets` | Save provider wallets for an avatar by username. |
| GET | `/api/Wallet/find-wallet` | Get the wallet that a public key belongs to. |
| POST | `/api/Wallet/send_token` | Send's a given token to the target provider. |
| GET | `/api/Wallet/supported-chains` | Get supported chains. |
| GET | `/api/Wallet/token/allowance` | Get token allowance for a spender Generic endpoint that works with any ERC-20 c |
| POST | `/api/Wallet/token/approve` | Approve token spending for a spender address Generic endpoint that works with a |
| GET | `/api/Wallet/token/balance` | Get token balance for an avatar Generic endpoint that works with any ERC-20 com |
| GET | `/api/Wallet/token/info` | Get token information (name, symbol, decimals, total supply) Generic endpoint t |
| POST | `/api/Wallet/transfer` | Transfer tokens between wallets. |
