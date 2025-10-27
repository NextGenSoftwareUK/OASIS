# WEB4 OASIS API - Complete Documentation

## 📋 **Overview**

The WEB4 OASIS API is the foundational data aggregation and identity layer that serves as the universal connector between all Web2 and Web3 technologies. It provides intelligent auto-failover, universal data aggregation, and comprehensive identity management.

## 🏗️ **Architecture**

### **Core Components**
- **OASIS HyperDrive**: Intelligent routing system (v1: auto-replication/failover; v2 adds auto-load balancing, AI optimization, analytics)
- **Avatar API**: Universal identity and profile management
- **Karma API**: Reputation and reward system
- **Data API**: Universal data storage and retrieval
- **Provider API**: Provider management and configuration

### **Key Features**
- **Auto-Failover**: Automatically switches between providers based on performance
- **Universal Data Aggregation**: Single API for all Web2/Web3 data sources
- **Identity Management**: Universal avatar system with cross-platform authentication
- **Karma System**: Reputation-based rewards and accountability
- **Privacy Control**: User controls data storage location and permissions

## 🔗 **Base URL**
```
https://api.oasisplatform.world
```

## 🔐 **Authentication**

### **API Key Authentication**
```http
Authorization: Bearer YOUR_API_KEY
```

### **Avatar Authentication**
```http
Authorization: Avatar YOUR_AVATAR_ID
```

## 📚 **API Endpoints**
### **HyperDrive API**

#### **Get/Set HyperDrive Mode**
```http
GET /api/hyperdrive/mode
PUT /api/hyperdrive/mode  (body: "Legacy" | "OASISHyperDrive2")
```

#### **Configuration**
```http
GET /api/hyperdrive/config
PUT /api/hyperdrive/config
POST /api/hyperdrive/config/validate
POST /api/hyperdrive/config/reset
```

#### **Metrics & Connections**
```http
GET /api/hyperdrive/metrics
GET /api/hyperdrive/metrics/{providerType}
POST /api/hyperdrive/metrics/{providerType}/reset
POST /api/hyperdrive/metrics/reset-all
GET /api/hyperdrive/connections
GET /api/hyperdrive/best-provider?strategy=Auto|RoundRobin|WeightedRoundRobin|LeastConnections|Geographic|CostBased|Performance
```

#### **Status & Health**
```http
GET /api/hyperdrive/status
```

#### **Analytics & AI**
```http
GET /api/hyperdrive/ai/recommendations
GET /api/hyperdrive/analytics/predictive/{providerType}?forecastDays=7
GET /api/hyperdrive/analytics/report?providerType={providerType}&timeRange=Last24Hours
GET /api/hyperdrive/dashboard
POST /api/hyperdrive/analytics/record
POST /api/hyperdrive/ai/record-performance
```

#### **Failover Management**
```http
GET /api/hyperdrive/failover/predictions
POST /api/hyperdrive/failover/record-failure
POST /api/hyperdrive/failover/preventive
POST /api/hyperdrive/failover/triggers
PUT /api/hyperdrive/failover/triggers/{id}
DELETE /api/hyperdrive/failover/triggers/{id}
GET /api/hyperdrive/failover/provider-rules
PUT /api/hyperdrive/failover/provider-rules
GET /api/hyperdrive/failover/escalation-rules
PUT /api/hyperdrive/failover/escalation-rules
```

#### **Cost Management**
```http
GET /api/hyperdrive/costs/current
GET /api/hyperdrive/costs/history?timeRange=Last30Days
GET /api/hyperdrive/costs/projections
PUT /api/hyperdrive/costs/limits
GET /api/hyperdrive/analytics/cost-optimization
```

#### **Recommendations**
```http
GET /api/hyperdrive/recommendations/smart
GET /api/hyperdrive/recommendations/security
GET /api/hyperdrive/analytics/performance-optimization
```

#### **Replication**
```http
GET /api/hyperdrive/replication/rules
PUT /api/hyperdrive/replication/rules
POST /api/hyperdrive/replication/triggers
PUT /api/hyperdrive/replication/triggers/{id}
DELETE /api/hyperdrive/replication/triggers/{id}
GET /api/hyperdrive/replication/provider-rules
PUT /api/hyperdrive/replication/provider-rules
GET /api/hyperdrive/replication/data-type-rules
PUT /api/hyperdrive/replication/data-type-rules
GET /api/hyperdrive/replication/schedule-rules
PUT /api/hyperdrive/replication/schedule-rules
GET /api/hyperdrive/replication/cost-optimization
PUT /api/hyperdrive/replication/cost-optimization
```

#### **Failover**
```http
GET /api/hyperdrive/failover/rules
PUT /api/hyperdrive/failover/rules
POST /api/hyperdrive/failover/triggers
PUT /api/hyperdrive/failover/triggers/{id}
DELETE /api/hyperdrive/failover/triggers/{id}
GET /api/hyperdrive/failover/provider-rules
PUT /api/hyperdrive/failover/provider-rules
GET /api/hyperdrive/failover/escalation-rules
PUT /api/hyperdrive/failover/escalation-rules
```

#### **Costs & Recommendations**
```http
GET /api/hyperdrive/costs/current
GET /api/hyperdrive/costs/history?timeRange=Last30Days
GET /api/hyperdrive/costs/projections
PUT /api/hyperdrive/costs/limits
GET /api/hyperdrive/recommendations/smart
GET /api/hyperdrive/recommendations/security
```

### **Core API**

#### **Get OASIS Status**
```http
GET /api/core/status
```

#### **Get OASIS Configuration**
```http
GET /api/core/config
```

#### **Get Provider Information**
```http
GET /api/core/providers
```

#### **Get OASIS Statistics**
```http
GET /api/core/stats
```

### **Avatar API**

#### **Authentication & Registration**
```http
POST /api/avatar/register
POST /api/avatar/register/{providerType}/{setGlobally}
GET /api/avatar/verify-email
GET /api/avatar/verify-email/{providerType}/{setGlobally}
POST /api/avatar/verify-email
POST /api/avatar/verify-email/{providerType}/{setGlobally}
POST /api/avatar/authenticate
POST /api/avatar/authenticate/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{AutoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
POST /api/avatar/authenticate-token/{JWTToken}
POST /api/avatar/authenticate-token/{JWTToken}/{providerType}/{setGlobally}
POST /api/avatar/refresh-token
POST /api/avatar/refresh-token/{providerType}/{setGlobally}
POST /api/avatar/revoke-token
POST /api/avatar/revoke-token/{providerType}/{setGlobally}
```

#### **Password Management**
```http
POST /api/avatar/forgot-password
POST /api/avatar/forgot-password/{providerType}/{setGlobally}
POST /api/avatar/validate-reset-token
POST /api/avatar/validate-reset-token/{providerType}/{setGlobally}
POST /api/avatar/reset-password
POST /api/avatar/reset-password/{providerType}/{setGlobally}
```

#### **Get All Avatars**
```http
GET /api/avatar/get-all-avatars
GET /api/avatar/get-all-avatars/{providerType}/{setGlobally}
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "username": "string",
      "email": "string",
      "firstName": "string",
      "lastName": "string",
      "karma": 1250,
      "level": "Advanced",
      "lastLoginDate": "2024-01-15T10:30:00Z",
      "createdDate": "2023-06-01T09:00:00Z",
      "isActive": true,
      "avatarType": "Human",
      "profileImage": "https://example.com/avatar.jpg"
    }
  ],
  "isError": false,
  "message": "Avatars loaded successfully"
}
```

#### **Get Avatar by ID**
```http
GET /api/avatar/get-avatar-detail-by-id/{id:guid}
GET /api/avatar/get-avatar-detail-by-id/{id:guid}/{providerType}/{setGlobally}
```

**Parameters:**
- `id` (string, required): Avatar UUID

**Response:**
```json
{
  "result": {
    "id": "uuid",
    "username": "string",
    "email": "string",
    "firstName": "string",
    "lastName": "string",
    "karma": 1250,
    "level": "Advanced",
    "lastLoginDate": "2024-01-15T10:30:00Z",
    "createdDate": "2023-06-01T09:00:00Z",
    "isActive": true,
    "avatarType": "Human",
    "profileImage": "https://example.com/avatar.jpg",
    "permissions": {
      "dataStorage": "user-controlled",
      "privacy": "private",
      "sharing": "selective"
    }
  },
  "isError": false,
  "message": "Avatar loaded successfully"
}
```

#### **Create Avatar**
```http
POST /api/avatar/register
POST /api/avatar/register/{providerType}/{setGlobally}
POST /api/avatar/create/{model}
POST /api/avatar/create/{model}/{providerType}/{setGlobally}
POST /api/avatar/save-avatar
POST /api/avatar/save-avatar/{providerType}/{setGlobally}
```

**Request Body:**
```json
{
  "username": "string",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "avatarType": "Human",
  "profileImage": "https://example.com/avatar.jpg"
}
```

**Response:**
```json
{
  "result": {
    "id": "uuid",
    "username": "string",
    "email": "string",
    "karma": 0,
    "level": "Beginner",
    "createdDate": "2024-01-15T10:30:00Z",
    "isActive": true
  },
  "isError": false,
  "message": "Avatar created successfully"
}
```

#### **Update Avatar**
```http
POST /api/avatar/update-avatar
POST /api/avatar/update-avatar/{providerType}/{setGlobally}
```

**Request Body:**
```json
{
  "id": "uuid",
  "username": "string",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "profileImage": "https://example.com/avatar.jpg"
}
```

#### **Delete Avatar**
```http
POST /api/avatar/delete-avatar/{id}
POST /api/avatar/delete-avatar/{id}/{providerType}/{setGlobally}
POST /api/avatar/soft-delete-avatar/{id}
POST /api/avatar/soft-delete-avatar/{id}/{providerType}/{setGlobally}
POST /api/avatar/undelete-avatar/{id}
POST /api/avatar/undelete-avatar/{id}/{providerType}/{setGlobally}
```

**Parameters:**
- `id` (string, required): Avatar UUID

### **Karma API**

#### **Get Karma for Avatar**
```http
GET /api/karma/get-karma-for-avatar/{avatarId}
GET /api/karma/get-karma-for-avatar/{avatarId}/{providerType}/{setGlobally}
```

#### **Get Karma Akashic Records**
```http
GET /api/karma/get-karma-akashic-records-for-avatar/{avatarId}
GET /api/karma/get-karma-akashic-records-for-avatar/{avatarId}/{providerType}/{setGlobally}
```

#### **Karma Weighting**
```http
GET /api/karma/get-positive-karma-weighting/{karmaType}
GET /api/karma/get-positive-karma-weighting/{karmaType}/{providerType}/{setGlobally}
GET /api/karma/get-negative-karma-weighting/{karmaType}
GET /api/karma/get-negative-karma-weighting/{karmaType}/{providerType}/{setGlobally}
```

#### **Vote for Karma Weighting**
```http
POST /api/karma/vote-for-positive-karma-weighting/{karmaType}/{weighting}
POST /api/karma/vote-for-positive-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}
POST /api/karma/vote-for-negative-karma-weighting/{karmaType}/{weighting}
POST /api/karma/vote-for-negative-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}
```

#### **Set Karma Weighting**
```http
POST /api/karma/set-positive-karma-weighting/{karmaType}/{weighting}
POST /api/karma/set-positive-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}
POST /api/karma/set-negative-karma-weighting/{karmaType}/{weighting}
POST /api/karma/set-negative-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}
```

#### **Add/Remove Karma**
```http
POST /api/karma/add-karma-to-avatar/{avatarId}
POST /api/karma/add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}
POST /api/karma/remove-karma-from-avatar/{avatarId}
POST /api/karma/remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}
```

#### **Karma Statistics & History**
```http
GET /api/karma/get-karma-stats/{avatarId}
GET /api/karma/get-karma-stats/{avatarId}/{providerType}/{setGlobally}
GET /api/karma/get-karma-history/{avatarId}?limit=50&offset=0
GET /api/karma/get-karma-history/{avatarId}/{providerType}/{setGlobally}?limit=50&offset=0
```

**Parameters:**
- `avatarId` (string, required): Avatar UUID

### **Data API**

#### **Holon Operations**
```http
POST /api/data/load-holon
GET /api/data/load-holon/{id}
GET /api/data/load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}
GET /api/data/load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}
GET /api/data/load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
```

#### **Load All Holons**
```http
POST /api/data/load-all-holons
GET /api/data/load-all-holons/{holonType}
GET /api/data/load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}
GET /api/data/load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}
GET /api/data/load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
```

#### **Load Holons for Parent**
```http
POST /api/data/load-holons-for-parent
GET /api/data/load-holons-for-parent/{id}/{holonType}
GET /api/data/load-holons-for-parent/{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}
GET /api/data/load-holons-for-parent/{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}
GET /api/data/load-holons-for-parent/{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
```

#### **Save Holon**
```http
POST /api/data/save-holon
POST /api/data/save-holon/{holon}
POST /api/data/save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}
POST /api/data/save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{providerType}/{setGlobally}
POST /api/data/save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{AutoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
POST /api/data/save-holon-off-chain
```

#### **Delete Holon**
```http
DELETE /api/data/delete-holon
DELETE /api/data/delete-holon/{id}
DELETE /api/data/delete-holon/{id}/{softDelete}
DELETE /api/data/delete-holon/{id}/{softDelete}/{providerType}/{setGlobally}
DELETE /api/data/delete-holon/{id}/{softDelete}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
```

#### **File Operations**
```http
POST /api/data/save-file
GET /api/data/save-file/{data}
GET /api/data/save-file/{data}/{providerType}/{setGlobally}
GET /api/data/save-file/{data}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
POST /api/data/load-file
GET /api/data/load-file/{id}
GET /api/data/load-file/{id}/{providerType}/{setGlobally}
GET /api/data/load-file/{id}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
```

#### **Data Operations**
```http
POST /api/data/save-data
GET /api/data/save-data/{key}/{value}
GET /api/data/save-data/{key}/{value}/{providerType}/{setGlobally}
GET /api/data/save-data/{key}/{value}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
POST /api/data/load-data
GET /api/data/load-data/{key}/{value}
GET /api/data/load-data/{key}/{value}/{providerType}/{setGlobally}
GET /api/data/load-data/{key}/{value}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
```

### **Wallet API**

#### **Wallet Management**
```http
GET /api/wallet/avatar/{id}/wallets
GET /api/wallet/avatar/username/{username}/wallets
GET /api/wallet/avatar/email/{email}/wallets
POST /api/wallet/avatar/{id}/wallets
POST /api/wallet/avatar/username/{username}/wallets
POST /api/wallet/avatar/email/{email}/wallets
```

#### **Default Wallet**
```http
GET /api/wallet/avatar/{id}/default-wallet
GET /api/wallet/avatar/username/{username}/default-wallet
GET /api/wallet/avatar/email/{email}/default-wallet
POST /api/wallet/avatar/{id}/default-wallet/{walletId}
POST /api/wallet/avatar/username/{username}/default-wallet/{walletId}
POST /api/wallet/avatar/email/{email}/default-wallet/{walletId}
```

#### **Import Operations**
```http
POST /api/wallet/avatar/{avatarId}/import/private-key
POST /api/wallet/avatar/username/{username}/import/private-key
POST /api/wallet/avatar/email/{email}/import/private-key
POST /api/wallet/avatar/{avatarId}/import/public-key
POST /api/wallet/avatar/username/{username}/import/public-key
POST /api/wallet/avatar/email/{email}/import/public-key
```

#### **Wallet Operations**
```http
GET /api/wallet/find-wallet
GET /api/wallet/avatar/{avatarId}/portfolio/value
GET /api/wallet/avatar/{avatarId}/wallets/chain/{chain}
POST /api/wallet/transfer
GET /api/wallet/avatar/{avatarId}/wallet/{walletId}/analytics
GET /api/wallet/supported-chains
GET /api/wallet/avatar/{avatarId}/wallet/{walletId}/tokens
```

### **Keys API**

#### **Key Management**
```http
POST /api/keys/clear_cache
POST /api/keys/link_provider_public_key_to_avatar_by_id
POST /api/keys/link_provider_public_key_to_avatar_by_username
POST /api/keys/link_provider_public_key_to_avatar_by_email
POST /api/keys/link_provider_private_key_to_avatar_by_id
POST /api/keys/link_provider_private_key_to_avatar_by_username
POST /api/keys/link_provider_private_key_to_avatar_by_email
```

#### **Key Generation**
```http
POST /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_id
POST /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_username
POST /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_email
POST /api/keys/generate_keypair_for_provider/{providerType}
POST /api/keys/generate_keypair/{keyPrefix}
```

#### **Key Retrieval**
```http
GET /api/keys/get_provider_unique_storage_key_for_avatar_by_id
GET /api/keys/get_provider_unique_storage_key_for_avatar_by_username
GET /api/keys/get_provider_unique_storage_key_for_avatar_by_email
GET /api/keys/get_provider_private_key_for_avatar_by_id
GET /api/keys/get_provider_private_key_for_avatar_by_username
GET /api/keys/get_provider_public_keys_for_avatar_by_id
GET /api/keys/get_provider_public_keys_for_avatar_by_username
GET /api/keys/get_provider_public_keys_for_avatar_by_email
```

#### **Key Operations**
```http
GET /api/keys/get_all_provider_public_keys_for_avatar_by_id/{id}
GET /api/keys/get_all_provider_public_keys_for_avatar_by_username/{username}
GET /api/keys/get_all_provider_private_keys_for_avatar_by_id/{id}
GET /api/keys/get_all_provider_private_keys_for_avatar_by_username/{username}
GET /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_id/{id}
GET /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_username/{username}
GET /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_email/{email}
```

#### **Key Lookup**
```http
GET /api/keys/get_avatar_id_for_provider_unique_storage_key/{providerKey}
GET /api/keys/get_avatar_username_for_provider_unique_storage_key/{providerKey}
GET /api/keys/get_avatar_email_for_provider_unique_storage_key/{providerKey}
GET /api/keys/get_avatar_for_provider_unique_storage_key/{providerKey}
GET /api/keys/get_avatar_id_for_provider_public_key/{providerKey}
GET /api/keys/get_avatar_username_for_provider_public_key/{providerKey}
GET /api/keys/get_avatar_email_for_provider_public_key/{providerKey}
GET /api/keys/get_avatar_for_provider_public_key/{providerKey}
GET /api/keys/get_avatar_id_for_provider_private_key/{providerKey}
GET /api/keys/get_avatar_username_for_provider_private_key/{providerKey}
GET /api/keys/get_avatar_for_provider_private_key/{providerKey}
```

#### **WiFi Operations**
```http
POST /api/keys/get_private_wifi/{source}
POST /api/keys/get_public_wifi
POST /api/keys/decode_private_wif/{data}
POST /api/keys/base58_check_decode/{data}
POST /api/keys/encode_signature/{source}
```

#### **Key CRUD**
```http
GET /api/keys/all
POST /api/keys/create
PUT /api/keys/{keyId}
DELETE /api/keys/{keyId}
GET /api/keys/stats
```

### **Files API**

#### **File Management**
```http
GET /api/files/get-all-files-stored-for-current-logged-in-avatar
POST /api/files/upload-file
GET /api/files/download-file/{fileId}
DELETE /api/files/delete-file/{fileId}
GET /api/files/file-metadata/{fileId}
PUT /api/files/update-file-metadata/{fileId}
```

### **Chat API**

#### **Chat Operations**
```http
POST /api/chat/start-new-chat-session
POST /api/chat/send-message/{sessionId}
GET /api/chat/history/{sessionId}
```

### **Competition API**

#### **Competition Management**
```http
GET /api/competition/leaderboard/{competitionType}/{seasonType}
GET /api/competition/my-rank/{competitionType}/{seasonType}
GET /api/competition/rank/{avatarId}/{competitionType}/{seasonType}
GET /api/competition/leagues/{competitionType}/{seasonType}
GET /api/competition/my-league/{competitionType}/{seasonType}
GET /api/competition/league/{avatarId}/{competitionType}/{seasonType}
GET /api/competition/tournaments
POST /api/competition/tournaments/{tournamentId}/join
GET /api/competition/stats/{competitionType}/{seasonType}
```

### **Gifts API**

#### **Gift Management**
```http
GET /api/gifts/my-gifts
POST /api/gifts/send-gift/{toAvatarId}
POST /api/gifts/receive-gift/{giftId}
POST /api/gifts/open-gift/{giftId}
GET /api/gifts/history
GET /api/gifts/stats
```

### **Map API**

#### **Map Operations**
```http
POST /api/map/CreateAndDrawRouteOnMapBetweenHolons/{holonDNA}
POST /api/map/CreateAndDrawRouteOnMapBeweenPoints/{points}
POST /api/map/Draw2DSpriteOnHUD/{sprite}/{x}/{y}
POST /api/map/Draw2DSpriteOnMap/{sprite}/{x}/{y}
POST /api/map/Draw3DObjectOnMap/{obj}/{x}/{y}
POST /api/map/HighlightBuildingOnMap/{building}
POST /api/map/PamMapDown/{value}
POST /api/map/PamMapLeft/{value}
POST /api/map/PamMapRight/{value}
POST /api/map/PamMapUp/{value}
POST /api/map/SelectBuildingOnMap/{building}
POST /api/map/SelectHolonOnMap/{holon}
POST /api/map/SelectQuestOnMap/{quest}
POST /api/map/ZoomMapIn/{value}
POST /api/map/ZoomMapOut/{value}
POST /api/map/ZoomToHolonOnMap/{holon}
POST /api/map/ZoomToQuestOnMap/{quest}
GET /api/map/nearby
POST /api/map/visit/{locationId}
GET /api/map/visit-history
GET /api/map/search-locations
GET /api/map/stats
```

### **Messaging API**

#### **Messaging Operations**
```http
POST /api/messaging/send-message-to-avatar/{toAvatarId}
GET /api/messaging/messages
GET /api/messaging/conversation/{otherAvatarId}
POST /api/messaging/mark-messages-read
GET /api/messaging/notifications
POST /api/messaging/mark-notifications-read
```

### **NFT API**

#### **NFT Operations**
```http
GET /api/nft
POST /api/nft
```

### **OAPP API**

#### **OAPP Management**
```http
GET /api/oapp
POST /api/oapp
```

### **OLand API**

#### **OLand Operations**
```http
GET /api/oland
POST /api/oland
```

### **ONET API**

#### **ONET Network Management**
```http
GET /api/onet/oasisdna
PUT /api/onet/oasisdna
GET /api/onet/network/status
GET /api/onet/network/nodes
POST /api/onet/network/connect
POST /api/onet/network/disconnect
GET /api/onet/network/stats
POST /api/onet/network/start
POST /api/onet/network/stop
GET /api/onet/network/topology
POST /api/onet/network/broadcast
```

### **ONODE API**

#### **ONODE Management**
```http
GET /api/onode/oasisdna
PUT /api/onode/oasisdna
GET /api/onode/status
GET /api/onode/info
POST /api/onode/start
POST /api/onode/stop
POST /api/onode/restart
GET /api/onode/metrics
GET /api/onode/logs
PUT /api/onode/config
GET /api/onode/config
GET /api/onode/peers
GET /api/onode/stats
```

### **Provider API**

#### **Provider Management**
```http
GET /api/provider/get-current-storage-provider
GET /api/provider/get-current-storage-provider-type
GET /api/provider/get-all-registered-providers
GET /api/provider/get-all-registered-provider-types
GET /api/provider/get-all-registered-providers-for-category/{category}
GET /api/provider/get-all-registered-storage-providers
GET /api/provider/get-all-registered-network-providers
GET /api/provider/get-all-registered-renderer-providers
GET /api/provider/get-registered-provider/{providerType}
GET /api/provider/is-provider-registered/{providerType}
GET /api/provider/get-providers-that-are-auto-replicating
GET /api/provider/get-providers-that-have-auto-fail-over-enabled
GET /api/provider/get-providers-that-have-auto-load-balance-enabled
```

#### **Provider Registration**
```http
POST /api/provider/register-provider/{provider}
POST /api/provider/register-provider-type/{providerType}
POST /api/provider/register-providers/{providers}
POST /api/provider/register-provider-types/{providerTypes}
POST /api/provider/unregister-provider/{provider}
POST /api/provider/unregister-provider-type/{providerType}
POST /api/provider/unregister-provider-types/{providerTypes}
POST /api/provider/unregister-providers/{providers}
```

#### **Provider Configuration**
```http
POST /api/provider/set-and-activate-current-storage-provider/{providerType}/{setGlobally}
POST /api/provider/activate-provider/{providerType}
POST /api/provider/deactivate-provider/{providerType}
POST /api/provider/set-auto-replicate-for-all-providers/{autoReplicate}
POST /api/provider/set-auto-replicate-for-list-of-providers/{autoReplicate}/{providerTypes}
POST /api/provider/set-auto-fail-over-for-all-providers/{addToFailOverList}
POST /api/provider/set-auto-fail-over-for-list-of-providers/{addToFailOverList}/{providerTypes}
POST /api/provider/set-auto-load-balance-for-all-providers/{addToLoadBalanceList}
POST /api/provider/set-auto-load-balance-for-list-of-providers/{addToLoadBalanceList}/{providerTypes}
POST /api/provider/set-provider-config/{providerType}/{connectionString}
```

### **Search API**

#### **Search Operations**
```http
GET /api/search/{searchParams}
GET /api/search/{searchParams}/{providerType}/{setGlobally}
```

### **Seeds API**

#### **Seeds Operations**
```http
GET /api/seeds/get-all-organisations
POST /api/seeds/pay-with-seeds-using-telos-account
POST /api/seeds/pay-with-seeds-using-avatar
POST /api/seeds/reward-with-seeds-using-telos-account
POST /api/seeds/reward-with-seeds-using-avatar
POST /api/seeds/donate-with-seeds-using-telos-account
POST /api/seeds/donate-with-seeds-using-avatar
POST /api/seeds/send-invite-to-join-seeds-using-telos-account
POST /api/seeds/send-invite-to-join-seeds-using-avatar
POST /api/seeds/accept-invite-to-join-seeds-using-telos-account
POST /api/seeds/accept-invite-to-join-seeds-using-avatar
```

#### **Seeds Account Management**
```http
GET /api/seeds/get-telos-account-names-for-avatar
GET /api/seeds/get-telos-account-private-key-for-avatar
GET /api/seeds/get-telos-account
GET /api/seeds/get-telos-account-for-avatar
GET /api/seeds/get-avatar-id-for-telos-account-name
GET /api/seeds/get-avatar-for-telos-account-name
GET /api/seeds/get-balance-for-telos-account
GET /api/seeds/get-balance-for-avatar
GET /api/seeds/generate-seeds-passport-signin-qrcode
GET /api/seeds/generate-seeds-passport-signin-qrcode-for-avatar
```

### **Settings API**

#### **Settings Management**
```http
GET /api/settings/get-all-settings-for-current-logged-in-avatar
GET /api/settings/hyperdrive-settings
PUT /api/settings/hyperdrive-settings
GET /api/settings/system-settings
PUT /api/settings/system-settings
GET /api/settings/subscription-settings
PUT /api/settings/subscription-settings
PUT /api/settings/update-settings
GET /api/settings/notification-preferences
PUT /api/settings/notification-preferences
GET /api/settings/privacy-settings
PUT /api/settings/privacy-settings
GET /api/settings/version
GET /api/settings/system-config
```

### **Share API**

#### **Share Operations**
```http
GET /api/share/share-holon/{holonId}/{avatarId}
GET /api/share/share-holon/{holonId}/{avatarIds}
```

### **Social API**

#### **Social Operations**
```http
GET /api/social/social-feed
POST /api/social/register-social-provider
POST /api/social/share-holon
GET /api/social/registered-providers
```

### **Solana API**

#### **Solana Operations**
```http
POST /api/solana
```

### **Stats API**

#### **Statistics**
```http
GET /api/stats/get-stats-for-current-logged-in-avatar
GET /api/stats/karma-stats/{avatarId}
GET /api/stats/karma-history/{avatarId}
GET /api/stats/gift-stats/{avatarId}
GET /api/stats/chat-stats/{avatarId}
GET /api/stats/key-stats/{avatarId}
GET /api/stats/leaderboard-stats/{avatarId}
GET /api/stats/system-stats
```

### **Subscription API**

#### **Subscription Management**
```http
GET /api/subscription/plans
POST /api/subscription/checkout/session
POST /api/subscription/webhooks/stripe
GET /api/subscription/subscriptions/me
GET /api/subscription/orders/me
POST /api/subscription/toggle-pay-as-you-go
GET /api/subscription/usage
POST /api/subscription/update-hyperdrive-config
GET /api/subscription/hyperdrive-usage
POST /api/subscription/check-hyperdrive-quota
```

### **Telos API**

#### **Telos Operations**
```http
GET /api/telos/get-telos-account-names-for-avatar
GET /api/telos/get-telos-accoun-private-key-for-avatar
GET /api/telos/get-telos-account
GET /api/telos/get-telos-account-for-avatar
GET /api/telos/get-avatar-id-for-telos-account-name
GET /api/telos/get-avatar-for-telos-account-name
GET /api/telos/get-balance-for-telos-account
GET /api/telos/get-balance-for-avatar
POST /api/telos/{avatarId}/{telosAccountName}
```

### **Video API**

#### **Video Operations**
```http
POST /api/video/start-video-call
POST /api/video/join-call/{callId}
POST /api/video/end-call/{callId}
```

### **EOSIO API**

#### **EOSIO Operations**
```http
GET /api/eosio/get-eosio-account-name-for-avatar
GET /api/eosio/get-eosio-account-private-key-for-avatar
GET /api/eosio/get-eosio-account
GET /api/eosio/get-eosio-account-for-avatar
GET /api/eosio/get-avatar-id-for-eosio-account-name
GET /api/eosio/get-avatar-for-eosio-account-name
GET /api/eosio/get-balance-for-eosio-account
GET /api/eosio/get-balance-for-avatar
POST /api/eosio/{avatarId}/{eosioAccountName}
```

### **Holochain API**

#### **Holochain Operations**
```http
GET /api/holochain/get-holochain-agentids-for-avatar
GET /api/holochain/get-holochain-agent-private-keys-for-avatar
GET /api/holochain/get-avatar-id-for-holochain-agentid
GET /api/holochain/get-avatar-for-holochain-agentid
GET /api/holochain/get-holo-fuel-balance-for-agentId
GET /api/holochain/get-holo-fuel-balance-for-avatar
POST /api/holochain/{avatarId}/{holochainAgentId}
```

### **Eggs API**

#### **Egg Operations**
```http
GET /api/eggs/get-all-eggs
GET /api/eggs/get-current-egg-quests
GET /api/eggs/get-current-egg-quest-leader-board
```

**Response:**
```json
{
  "result": {
    "avatarId": "uuid",
    "totalKarma": 1250,
    "level": "Advanced",
    "rank": 42,
    "karmaHistory": [
      {
        "action": "Recycling",
        "karma": 50,
        "timestamp": "2024-01-15T10:30:00Z",
        "source": "Environmental App"
      }
    ],
    "rewards": [
      {
        "name": "Free Smoothie",
        "cost": 100,
        "redeemed": false
      }
    ]
  },
  "isError": false,
  "message": "Karma loaded successfully"
}
```

#### **Add Karma**
```http
POST /karma/add-karma
```

**Request Body:**
```json
{
  "avatarId": "uuid",
  "action": "string",
  "karma": 50,
  "source": "string",
  "description": "string"
}
```

#### **Get Karma Leaderboard**
```http
GET /karma/get-karma-leaderboard?limit=50
```

**Parameters:**
- `limit` (integer, optional): Number of results (default: 50)

**Response:**
```json
{
  "result": [
    {
      "avatarId": "uuid",
      "avatarName": "string",
      "email": "string",
      "totalKarma": 5000,
      "karmaLevel": "Legendary",
      "lastActivity": "2024-01-15T10:30:00Z",
      "rank": 1
    }
  ],
  "isError": false,
  "message": "Karma leaderboard loaded successfully"
}
```

#### **Get Karma History**
```http
GET /karma/get-karma-history/{id}?limit=100
```

**Parameters:**
- `id` (string, required): Avatar UUID
- `limit` (integer, optional): Number of results (default: 100)

### **Data API**

#### **Get My Data Files**
```http
GET /data/get-my-data-files
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "name": "string",
      "type": "document",
      "size": "2.4 MB",
      "lastModified": "2024-01-15T14:30:00Z",
      "permissions": "private",
      "storageNodes": ["Node-1", "Node-3"],
      "replication": 3,
      "encryption": true,
      "checksum": "sha256:abc123..."
    }
  ],
  "isError": false,
  "message": "Data files loaded successfully"
}
```

#### **Upload File**
```http
POST /data/upload-file
```

**Request Body (multipart/form-data):**
```
file: [binary data]
name: string
type: string
permissions: string
storageNodes: string[]
replication: integer
```

**Response:**
```json
{
  "result": {
    "id": "uuid",
    "name": "string",
    "size": "2.4 MB",
    "uploadedDate": "2024-01-15T14:30:00Z",
    "storageNodes": ["Node-1", "Node-3"],
    "replication": 3
  },
  "isError": false,
  "message": "File uploaded successfully"
}
```

#### **Update File Permissions**
```http
PUT /data/update-file-permissions
```

**Request Body:**
```json
{
  "fileId": "uuid",
  "permissions": "shared",
  "allowedUsers": ["uuid1", "uuid2"],
  "storageNodes": ["Node-1", "Node-2", "Node-3"]
}
```

#### **Delete File**
```http
DELETE /data/delete-file/{id}
```

**Parameters:**
- `id` (string, required): File UUID

### **NFT API**

#### **Get NFT by ID**
```http
GET /api/nft/load-nft-by-id/{id}
```

#### **Get NFT by Hash**
```http
GET /api/nft/load-nft-by-hash/{hash}
```

#### **Get All NFTs for Avatar**
```http
GET /api/nft/load-all-nfts-for_avatar/{avatarId}
```

#### **Get All NFTs for Mint Address**
```http
GET /api/nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress}
```

#### **Get All GeoNFTs for Avatar**
```http
GET /api/nft/load-all-geo-nfts-for-avatar/{avatarId}
```

#### **Get All GeoNFTs for Mint Address**
```http
GET /api/nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}
```

#### **Get All NFTs**
```http
GET /api/nft/load-all-nfts
```

#### **Get All GeoNFTs**
```http
GET /api/nft/load-all-geo-nfts
```

#### **Send NFT**
```http
POST /api/nft/send-nft
```

#### **Mint NFT**
```http
POST /api/nft/mint-nft
```

#### **Place GeoNFT**
```http
POST /api/nft/place-geo-nft
```

#### **Mint and Place GeoNFT**
```http
POST /api/nft/mint-and-place-geo-nft
```

#### **Get NFT Provider**
```http
GET /api/nft/get-nft-provider-from-provider-type/{providerType}
```

### **Search API**

#### **Search**
```http
GET /api/search
```

#### **Advanced Search**
```http
GET /api/search/advanced
```

### **Wallet API**

#### **Send Token**
```http
POST /api/wallet/send-token
```

#### **Get Wallet Balance**
```http
GET /api/wallet/balance
```

#### **Get Wallet Transactions**
```http
GET /api/wallet/transactions
```

### **Keys API**

#### **Link Telos Account**
```http
POST /api/keys/link-telos-account
```

#### **Link EOSIO Account**
```http
POST /api/keys/link-eosio-account
```

#### **Link Holochain Agent**
```http
POST /api/keys/link-holochain-agent
```

#### **Get Provider Key**
```http
GET /api/keys/provider-key/{avatarUsername}/{providerType}
```

### **OLand API**

#### **Get OLand Price**
```http
GET /api/oland/price/{count}
```

#### **Purchase OLand**
```http
POST /api/oland/purchase
```

#### **Get All OLand**
```http
GET /api/oland/load-all
```

#### **Get OLand by ID**
```http
GET /api/oland/{olandId}
```

#### **Delete OLand**
```http
DELETE /api/oland/{avatarId}/{olandId}
```

#### **Save OLand**
```http
POST /api/oland/save
```

#### **Update OLand**
```http
PUT /api/oland/update
```

### **Files API**

#### **Get All Files for Avatar**
```http
GET /api/files/avatar-files
```

#### **Upload File**
```http
POST /api/files/upload
```

#### **Download File**
```http
GET /api/files/download/{fileId}
```

#### **Delete File**
```http
DELETE /api/files/{fileId}
```

### **Chat API**

#### **Send Message**
```http
POST /api/chat/send
```

#### **Get Messages**
```http
GET /api/chat/messages
```

#### **Get Chat Rooms**
```http
GET /api/chat/rooms
```

#### **Create Chat Room**
```http
POST /api/chat/rooms
```

### **Messaging API**

#### **Send Message**
```http
POST /api/messaging/send
```

#### **Get Messages**
```http
GET /api/messaging/messages
```

#### **Get Conversations**
```http
GET /api/messaging/conversations
```

### **Social API**

#### **Get Friends**
```http
GET /api/social/friends
```

#### **Add Friend**
```http
POST /api/social/friends
```

#### **Remove Friend**
```http
DELETE /api/social/friends/{friendId}
```

#### **Get Followers**
```http
GET /api/social/followers
```

#### **Follow User**
```http
POST /api/social/follow
```

#### **Unfollow User**
```http
DELETE /api/social/follow/{userId}
```

### **Share API**

#### **Share Content**
```http
POST /api/share/content
```

#### **Get Shared Content**
```http
GET /api/share/content
```

#### **Get Share Statistics**
```http
GET /api/share/stats
```

### **Settings API**

#### **Get User Settings**
```http
GET /api/settings/user
```

#### **Update User Settings**
```http
PUT /api/settings/user
```

#### **Get Privacy Settings**
```http
GET /api/settings/privacy
```

#### **Update Privacy Settings**
```http
PUT /api/settings/privacy
```

### **Seeds API**

#### **Get Seeds Balance**
```http
GET /api/seeds/balance
```

#### **Get Seeds Transactions**
```http
GET /api/seeds/transactions
```

#### **Transfer Seeds**
```http
POST /api/seeds/transfer
```

### **Stats API**

#### **Get User Statistics**
```http
GET /api/stats/user
```

#### **Get System Statistics**
```http
GET /api/stats/system
```

#### **Get Provider Statistics**
```http
GET /api/stats/provider
```

### **Video API**

#### **Upload Video**
```http
POST /api/video/upload
```

#### **Get Video**
```http
GET /api/video/{videoId}
```

#### **Get User Videos**
```http
GET /api/video/user-videos
```

#### **Delete Video**
```http
DELETE /api/video/{videoId}
```

### **Solana API**

#### **Mint NFT**
```http
POST /api/solana/mint
```

#### **Send Transaction**
```http
POST /api/solana/send
```

#### **Get Balance**
```http
GET /api/solana/balance
```

### **Telos API**

#### **Get Telos Account**
```http
GET /api/telos/account
```

#### **Get Telos Balance**
```http
GET /api/telos/balance
```

#### **Send Telos Transaction**
```http
POST /api/telos/send
```

### **Holochain API**

#### **Get Holochain Agent**
```http
GET /api/holochain/agent
```

#### **Get Holochain Data**
```http
GET /api/holochain/data
```

#### **Store Holochain Data**
```http
POST /api/holochain/store
```

### **EOSIO API**

#### **Get EOSIO Account**
```http
GET /api/eosio/account
```

#### **Get EOSIO Balance**
```http
GET /api/eosio/balance
```

#### **Send EOSIO Transaction**
```http
POST /api/eosio/send
```

### **Mission API**

#### **Get All Missions**
```http
GET /api/mission/missions
```

#### **Get Mission by ID**
```http
GET /api/mission/{missionId}
```

#### **Create Mission**
```http
POST /api/mission/create
```

#### **Update Mission**
```http
PUT /api/mission/{missionId}
```

#### **Delete Mission**
```http
DELETE /api/mission/{missionId}
```

### **Quest API**

#### **Get All Quests**
```http
GET /api/quest/quests
```

#### **Get Quest by ID**
```http
GET /api/quest/{questId}
```

#### **Create Quest**
```http
POST /api/quest/create
```

#### **Update Quest**
```http
PUT /api/quest/{questId}
```

#### **Delete Quest**
```http
DELETE /api/quest/{questId}
```

### **Map API**

#### **Get Map Data**
```http
GET /api/map/data
```

#### **Update Map Data**
```http
PUT /api/map/data
```

#### **Get Map Layers**
```http
GET /api/map/layers
```

### **OAPP API**

#### **Get All OAPPs**
```http
GET /api/oapp/oapps
```

#### **Get OAPP by ID**
```http
GET /api/oapp/{oappId}
```

#### **Create OAPP**
```http
POST /api/oapp/create
```

#### **Update OAPP**
```http
PUT /api/oapp/{oappId}
```

#### **Delete OAPP**
```http
DELETE /api/oapp/{oappId}
```

### **Cargo API**

#### **Get Cargo Orders**
```http
GET /api/cargo/orders
```

#### **Create Cargo Order**
```http
POST /api/cargo/orders
```

#### **Update Cargo Order**
```http
PUT /api/cargo/orders/{orderId}
```

#### **Delete Cargo Order**
```http
DELETE /api/cargo/orders/{orderId}
```

### **Gifts API**

#### **Get All Gifts**
```http
GET /api/gifts/all
```

#### **Send Gift**
```http
POST /api/gifts/send
```

#### **Receive Gift**
```http
POST /api/gifts/receive
```

### **Eggs API**

#### **Get All Eggs**
```http
GET /api/eggs/all
```

#### **Get Current Egg Quests**
```http
GET /api/eggs/quests
```

#### **Get Egg Quest Leaderboard**
```http
GET /api/eggs/leaderboard
```

### **Provider API**

#### **Get All Providers**
```http
GET /provider/get-all-providers
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "name": "Ethereum",
      "type": "blockchain",
      "status": "active",
      "performance": {
        "speed": 95,
        "reliability": 98,
        "cost": 75
      },
      "endpoints": ["https://mainnet.infura.io"],
      "supportedFeatures": ["smart-contracts", "tokens", "nfts"]
    }
  ],
  "isError": false,
  "message": "Providers loaded successfully"
}
```

#### **Register Provider**
```http
POST /provider/register-provider
```

**Request Body:**
```json
{
  "name": "string",
  "type": "blockchain|database|storage|cloud",
  "endpoints": ["string"],
  "configuration": {
    "apiKey": "string",
    "networkId": "string"
  },
  "supportedFeatures": ["string"]
}
```

#### **Update Provider**
```http
PUT /provider/update-provider
```

**Request Body:**
```json
{
  "id": "uuid",
  "name": "string",
  "status": "active|inactive|maintenance",
  "configuration": {
    "apiKey": "string",
    "networkId": "string"
  }
}
```

#### **Delete Provider**
```http
DELETE /provider/delete-provider/{id}
```

**Parameters:**
- `id` (string, required): Provider UUID

### **Keys API**

#### **Get All Keys**
```http
GET /keys/get-all-keys
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "name": "string",
      "type": "encryption|signing|authentication",
      "algorithm": "RSA|ECDSA|AES",
      "keySize": 2048,
      "publicKey": "string",
      "privateKey": "string",
      "createdDate": "2024-01-15T10:30:00Z",
      "lastUsed": "2024-01-15T14:30:00Z",
      "isActive": true,
      "permissions": ["read", "write", "sign"]
    }
  ],
  "isError": false,
  "message": "Keys loaded successfully"
}
```

#### **Generate New Key**
```http
POST /keys/generate-key
```

**Request Body:**
```json
{
  "name": "string",
  "type": "encryption|signing|authentication",
  "algorithm": "RSA|ECDSA|AES",
  "keySize": 2048,
  "permissions": ["read", "write", "sign"]
}
```

#### **Get Key by ID**
```http
GET /keys/get-key/{id}
```

#### **Update Key**
```http
PUT /keys/update-key
```

#### **Delete Key**
```http
DELETE /keys/delete-key/{id}
```

### **Wallets API**

#### **Get All Wallets**
```http
GET /wallets/get-all-wallets
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "name": "string",
      "type": "ethereum|bitcoin|solana|eosio",
      "address": "string",
      "balance": {
        "amount": 1000.50,
        "currency": "ETH",
        "usdValue": 2500.75
      },
      "isActive": true,
      "createdDate": "2024-01-15T10:30:00Z",
      "lastTransaction": "2024-01-15T14:30:00Z",
      "transactions": 25,
      "metadata": {
        "network": "mainnet",
        "derivationPath": "m/44'/60'/0'/0/0"
      }
    }
  ],
  "isError": false,
  "message": "Wallets loaded successfully"
}
```

#### **Create Wallet**
```http
POST /wallets/create-wallet
```

**Request Body:**
```json
{
  "name": "string",
  "type": "ethereum|bitcoin|solana|eosio",
  "network": "mainnet|testnet",
  "derivationPath": "m/44'/60'/0'/0/0"
}
```

#### **Get Wallet by ID**
```http
GET /wallets/get-wallet/{id}
```

#### **Update Wallet**
```http
PUT /wallets/update-wallet
```

#### **Delete Wallet**
```http
DELETE /wallets/delete-wallet/{id}
```

### **Search API**

#### **Search Data**
```http
GET /search/search-data?query={query}&type={type}&limit={limit}
```

**Parameters:**
- `query` (string, required): Search query
- `type` (string, optional): Data type filter
- `limit` (integer, optional): Maximum results (default: 50)

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "type": "avatar|holon|nft|data",
      "title": "string",
      "description": "string",
      "score": 0.95,
      "metadata": {
        "createdDate": "2024-01-15T10:30:00Z",
        "tags": ["string"],
        "category": "string"
      }
    }
  ],
  "isError": false,
  "message": "Search completed successfully",
  "totalResults": 150,
  "page": 1,
  "pageSize": 50
}
```

#### **Advanced Search**
```http
POST /search/advanced-search
```

**Request Body:**
```json
{
  "query": "string",
  "filters": {
    "type": ["avatar", "holon"],
    "dateRange": {
      "start": "2024-01-01T00:00:00Z",
      "end": "2024-12-31T23:59:59Z"
    },
    "tags": ["string"],
    "category": "string"
  },
  "sortBy": "relevance|date|title",
  "sortOrder": "asc|desc",
  "page": 1,
  "pageSize": 50
}
```

### **OLands API**

#### **Get All OLands**
```http
GET /olands/get-all-olands
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "name": "string",
      "description": "string",
      "coordinates": {
        "latitude": 40.7128,
        "longitude": -74.0060,
        "altitude": 10.5
      },
      "size": {
        "width": 1000,
        "height": 1000,
        "depth": 50
      },
      "owner": "uuid",
      "createdDate": "2024-01-15T10:30:00Z",
      "lastModified": "2024-01-15T14:30:00Z",
      "isPublic": true,
      "price": {
        "amount": 1000,
        "currency": "HERZ"
      },
      "metadata": {
        "biome": "forest|desert|ocean|mountain",
        "resources": ["wood", "stone", "water"],
        "structures": ["house", "farm", "factory"]
      }
    }
  ],
  "isError": false,
  "message": "OLands loaded successfully"
}
```

#### **Get OLand by ID**
```http
GET /olands/get-oland/{id}
```

#### **Create OLand**
```http
POST /olands/create-oland
```

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "coordinates": {
    "latitude": 40.7128,
    "longitude": -74.0060,
    "altitude": 10.5
  },
  "size": {
    "width": 1000,
    "height": 1000,
    "depth": 50
  },
  "isPublic": true,
  "price": {
    "amount": 1000,
    "currency": "HERZ"
  },
  "metadata": {
    "biome": "forest|desert|ocean|mountain",
    "resources": ["string"],
    "structures": ["string"]
  }
}
```

#### **Update OLand**
```http
PUT /olands/update-oland
```

#### **Delete OLand**
```http
DELETE /olands/delete-oland/{id}
```

### **NFTs API (Basic)**

#### **Get All NFTs**
```http
GET /nfts/get-all-nfts
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "name": "string",
      "description": "string",
      "image": "https://example.com/image.jpg",
      "type": "artwork|collectible|utility",
      "rarity": "common|uncommon|rare|epic|legendary",
      "owner": "uuid",
      "creator": "uuid",
      "createdDate": "2024-01-15T10:30:00Z",
      "lastModified": "2024-01-15T14:30:00Z",
      "price": {
        "amount": 1000,
        "currency": "HERZ"
      },
      "metadata": {
        "blockchain": "ethereum",
        "tokenId": "12345",
        "contractAddress": "0x...",
        "ipfsHash": "Qm..."
      },
      "isTradeable": true,
      "isTransferable": true
    }
  ],
  "isError": false,
  "message": "NFTs loaded successfully"
}
```

#### **Get NFT by ID**
```http
GET /nfts/get-nft/{id}
```

#### **Create NFT**
```http
POST /nfts/create-nft
```

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "image": "https://example.com/image.jpg",
  "type": "artwork|collectible|utility",
  "rarity": "common|uncommon|rare|epic|legendary",
  "price": {
    "amount": 1000,
    "currency": "HERZ"
  },
  "metadata": {
    "blockchain": "ethereum",
    "contractAddress": "0x...",
    "ipfsHash": "Qm..."
  },
  "isTradeable": true,
  "isTransferable": true
}
```

#### **Update NFT**
```http
PUT /nfts/update-nft
```

#### **Delete NFT**
```http
DELETE /nfts/delete-nft/{id}
```

## 🔧 **OASIS HyperDrive**

### **Auto-Failover System**

The OASIS HyperDrive automatically routes requests to the optimal provider based on:

- **Performance**: Response time and throughput
- **Cost**: Transaction fees and storage costs
- **Reliability**: Uptime and error rates
- **Geographic Proximity**: Distance to user location

### **Provider Selection Algorithm**

```javascript
// Example provider selection logic
function selectOptimalProvider(request) {
  const providers = getAvailableProviders();
  const scores = providers.map(provider => ({
    provider,
    score: calculateScore(provider, request)
  }));
  
  return scores.sort((a, b) => b.score - a.score)[0].provider;
}

function calculateScore(provider, request) {
  const performance = provider.metrics.speed * 0.4;
  const cost = (100 - provider.metrics.cost) * 0.3;
  const reliability = provider.metrics.reliability * 0.2;
  const proximity = calculateProximity(provider, request.userLocation) * 0.1;
  
  return performance + cost + reliability + proximity;
}
```

### **Auto-Replication**

Data is automatically replicated across multiple providers:

```json
{
  "replication": {
    "strategy": "intelligent",
    "minReplicas": 3,
    "maxReplicas": 5,
    "providers": ["ethereum", "ipfs", "mongodb"],
    "syncInterval": "5m",
    "conflictResolution": "last-write-wins"
  }
}
```

## 🔐 **Security & Privacy**

### **Data Encryption**
- **Transport**: TLS 1.3 for all API communications
- **Storage**: AES-256 encryption for data at rest
- **Keys**: User-controlled encryption keys

### **Privacy Controls**
```json
{
  "privacy": {
    "dataStorage": "user-controlled",
    "location": ["Node-1", "Node-2", "Node-3"],
    "permissions": {
      "read": ["user", "admin"],
      "write": ["user"],
      "delete": ["user"]
    },
    "anonymization": true,
    "auditLog": true
  }
}
```

### **Access Control**
- **Role-based permissions**: User, Admin, System
- **Granular permissions**: Field-level access control
- **Audit logging**: Complete access history

## 📊 **Error Handling**

### **Standard Error Response**
```json
{
  "isError": true,
  "message": "Error description",
  "errorCode": "AVATAR_NOT_FOUND",
  "details": {
    "field": "id",
    "value": "invalid-uuid",
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "exception": {
    "type": "ArgumentException",
    "stackTrace": "..."
  }
}
```

### **Common Error Codes**
- `AVATAR_NOT_FOUND`: Avatar with specified ID not found
- `INVALID_PERMISSIONS`: Insufficient permissions for operation
- `PROVIDER_UNAVAILABLE`: Requested provider is currently unavailable
- `RATE_LIMIT_EXCEEDED`: API rate limit exceeded
- `VALIDATION_ERROR`: Request validation failed

## 📈 **Rate Limits**

### **Standard Limits**
- **Avatar API**: 1000 requests/hour
- **Karma API**: 500 requests/hour
- **Data API**: 100 requests/hour
- **Provider API**: 200 requests/hour

### **Rate Limit Headers**
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642248000
```

## 🔄 **Webhooks**

### **Supported Events**
- `avatar.created`
- `avatar.updated`
- `karma.earned`
- `data.uploaded`
- `provider.status_changed`

### **Webhook Configuration**
```json
{
  "url": "https://your-app.com/webhooks/oasis",
  "events": ["avatar.created", "karma.earned"],
  "secret": "your-webhook-secret",
  "retryPolicy": {
    "maxRetries": 3,
    "backoffMultiplier": 2
  }
}
```

## 📱 **SDKs**

### **JavaScript/Node.js**
```bash
npm install @oasis/api-client
```

```javascript
import { OASISClient } from '@oasis/api-client';

const client = new OASISClient({
  apiKey: 'your-api-key',
  baseUrl: 'https://api.oasisplatform.world'
});

// Get avatar
const avatar = await client.avatar.get('avatar-id');

// Add karma
await client.karma.add({
  avatarId: 'avatar-id',
  action: 'Recycling',
  karma: 50
});
```

### **C#/.NET**
```bash
dotnet add package OASIS.API.Client
```

```csharp
using OASIS.API.Client;

var client = new OASISClient("your-api-key");

// Get avatar
var avatar = await client.Avatar.GetAsync("avatar-id");

// Add karma
await client.Karma.AddAsync(new KarmaRequest
{
    AvatarId = "avatar-id",
    Action = "Recycling",
    Karma = 50
});
```

### **Python**
```bash
pip install oasis-api-client
```

```python
from oasis_api import OASISClient

client = OASISClient(api_key='your-api-key')

# Get avatar
avatar = client.avatar.get('avatar-id')

# Add karma
client.karma.add({
    'avatar_id': 'avatar-id',
    'action': 'Recycling',
    'karma': 50
})
```

## 🧪 **Testing**

### **Sandbox Environment**
```
https://sandbox-api.oasisplatform.world
```

### **Test Data**
```json
{
  "testAvatar": {
    "id": "test-avatar-id",
    "username": "testuser",
    "email": "test@example.com"
  },
  "testProviders": [
    "ethereum-testnet",
    "mongodb-test",
    "ipfs-test"
  ]
}
```

## 📚 **Examples**

### **Complete Avatar Management**
```javascript
// Create avatar
const avatar = await client.avatar.create({
  username: 'newuser',
  email: 'user@example.com',
  firstName: 'John',
  lastName: 'Doe'
});

// Add karma
await client.karma.add({
  avatarId: avatar.id,
  action: 'Account Creation',
  karma: 100,
  source: 'OASIS Platform'
});

// Upload file
const file = await client.data.upload({
  file: fileData,
  name: 'profile.jpg',
  permissions: 'private'
});
```

### **Advanced Provider Management**
```javascript
// Register multiple providers
const providers = await Promise.all([
  client.provider.register({
    name: 'Ethereum Mainnet',
    type: 'blockchain',
    endpoints: ['https://mainnet.infura.io'],
    configuration: { apiKey: 'your-infura-key' }
  }),
  client.provider.register({
    name: 'IPFS Cluster',
    type: 'storage',
    endpoints: ['https://ipfs.io', 'https://gateway.pinata.cloud'],
    configuration: { apiKey: 'your-pinata-key' }
  }),
  client.provider.register({
    name: 'MongoDB Atlas',
    type: 'database',
    endpoints: ['mongodb+srv://cluster.mongodb.net'],
    configuration: { connectionString: 'your-connection-string' }
  })
]);

// Monitor provider performance
const metrics = await Promise.all(
  providers.map(p => client.provider.getMetrics(p.id))
);

// Auto-failover will use the best performing provider
console.log('Provider performance:', metrics);
```

### **Universal Data Management**
```javascript
// Store data with automatic provider selection
const data = await client.data.store({
  key: 'user-preferences',
  value: {
    theme: 'dark',
    language: 'en',
    notifications: true
  },
  metadata: {
    type: 'user-settings',
    version: '1.0',
    tags: ['preferences', 'ui']
  }
});

// Search across all data sources
const results = await client.search.advancedSearch({
  query: 'user preferences',
  filters: {
    type: ['data'],
    tags: ['preferences'],
    dateRange: {
      start: '2024-01-01T00:00:00Z',
      end: '2024-12-31T23:59:59Z'
    }
  }
});

// Data is automatically replicated across providers
console.log('Data stored and searchable:', results);
```

### **Key and Wallet Management**
```javascript
// Generate encryption keys
const encryptionKey = await client.keys.generate({
  name: 'User Encryption Key',
  type: 'encryption',
  algorithm: 'RSA',
  keySize: 2048
});

// Create multi-chain wallet
const wallet = await client.wallets.create({
  name: 'My Multi-Chain Wallet',
  type: 'ethereum',
  network: 'mainnet'
});

// Add additional blockchain support
await client.wallets.addChain(wallet.id, {
  type: 'solana',
  network: 'mainnet'
});

// Secure data with user's encryption key
const encryptedData = await client.data.encrypt({
  data: 'sensitive information',
  keyId: encryptionKey.id
});
```

### **Integration with WEB5 STAR API**
```javascript
// Initialize OASIS for STAR integration
const oasis = new OASISClient('your-oasis-key');
await oasis.boot();

// Create avatar that will be used by STAR
const avatar = await oasis.avatar.create({
  username: 'starplayer',
  email: 'player@example.com'
});

// Initialize STAR with OASIS integration
const star = new STARClient('your-star-key', oasis);

// STAR automatically inherits avatar and karma system
const mission = await star.missions.create({
  name: 'OASIS Integration Test',
  karmaReward: 500
});

// Karma earned in STAR is tracked in OASIS
await star.missions.complete(mission.id);
const updatedAvatar = await oasis.avatar.get(avatar.id);
console.log(`Karma after mission: ${updatedAvatar.karma}`);
```

### **Provider Management**
```javascript
// Register new provider
const provider = await client.provider.register({
  name: 'Custom Blockchain',
  type: 'blockchain',
  endpoints: ['https://custom-blockchain.com'],
  configuration: {
    apiKey: 'your-api-key',
    networkId: 'mainnet'
  }
});

// Monitor provider performance
const metrics = await client.provider.getMetrics(provider.id);
console.log(`Performance: ${metrics.performance}%`);
```

## 🚀 **Getting Started**

1. **Sign up** for an API key at [oasisplatform.world](https://oasisplatform.world)
2. **Choose your SDK** from the available options
3. **Read the examples** to understand the API structure
4. **Start building** with the sandbox environment
5. **Deploy** to production when ready

## 📞 **Support**

- **Documentation**: [docs.oasisplatform.world](https://docs.oasisplatform.world)
- **Community**: [Discord](https://discord.gg/oasis)
- **GitHub**: [github.com/oasisplatform](https://github.com/oasisplatform)
- **Email**: support@oasisplatform.world

---

*This documentation is updated regularly. For the latest version, visit [docs.oasisplatform.world](https://docs.oasisplatform.world)*
