# WEB4 OASIS API - Complete Endpoints Reference

## 📋 **Overview**

This document provides a comprehensive reference of ALL WEB4 OASIS API endpoints, organized by controller and functionality. This is the definitive guide for all available endpoints in the OASIS Web API.

## 🔗 **Base URLs**

### **Development**
```
https://localhost:5000/api
```

### **Production**
```
https://api.oasisplatform.world
```

## 🔐 **Authentication**

### **API Key Authentication**
```http
Authorization: Bearer YOUR_OASIS_API_KEY
```

### **Avatar Authentication**
```http
Authorization: Avatar YOUR_AVATAR_ID
```

## 📚 **Complete API Endpoints**

### **Core Controllers**
```http
# ONODE Controller
GET    /api/onode/status              # Get ONODE status
GET    /api/onode/config             # Get ONODE configuration
GET    /api/onode/providers          # Get provider information
GET    /api/onode/stats              # Get ONODE statistics

# ONET Controller  
GET    /api/onet/status              # Get ONET network status
GET    /api/onet/topology            # Get network topology
GET    /api/onet/nodes               # Get connected nodes
POST   /api/onet/connect             # Connect to ONET node
POST   /api/onet/disconnect          # Disconnect from ONET node

# Settings Controller
GET    /api/settings                 # Get system settings
PUT    /api/settings                 # Update system settings
```

### **Avatar Controller**
```http
# Avatar Registration & Authentication
POST   /api/avatar/register                            # Register new avatar
POST   /api/avatar/register/{providerType}/{setGlobally} # Register with specific provider
GET    /api/avatar/verify-email                        # Verify email (GET)
POST   /api/avatar/verify-email                        # Verify email (POST)
GET    /api/avatar/verify-email/{providerType}/{setGlobally} # Verify with provider
POST   /api/avatar/verify-email/{providerType}/{setGlobally} # Verify with provider (POST)
POST   /api/avatar/authenticate                        # Authenticate avatar
POST   /api/avatar/authenticate/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{AutoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings} # Advanced authentication
POST   /api/avatar/authenticate-token/{JWTToken}        # Authenticate with JWT token
POST   /api/avatar/authenticate-token/{JWTToken}/{providerType}/{setGlobally} # Authenticate JWT with provider
POST   /api/avatar/refresh-token                       # Refresh authentication token
POST   /api/avatar/refresh-token/{providerType}/{setGlobally} # Refresh token with provider
POST   /api/avatar/revoke-token                        # Revoke authentication token
POST   /api/avatar/revoke-token/{providerType}/{setGlobally} # Revoke token with provider

# Password Management
POST   /api/avatar/forgot-password                     # Request password reset
POST   /api/avatar/forgot-password/{providerType}/{setGlobally} # Forgot password with provider
POST   /api/avatar/validate-reset-token                # Validate password reset token
POST   /api/avatar/validate-reset-token/{providerType}/{setGlobally} # Validate reset token with provider
POST   /api/avatar/reset-password                      # Reset password
POST   /api/avatar/reset-password/{providerType}/{setGlobally} # Reset password with provider

# Avatar CRUD Operations
POST   /api/avatar/create/{model}                      # Create avatar
POST   /api/avatar/create/{model}/{providerType}/{setGlobally} # Create avatar with provider
GET    /api/avatar/get-terms                           # Get terms and conditions

# Avatar Portraits
GET    /api/avatar/get-avatar-portrait/{id}            # Get avatar portrait by ID
GET    /api/avatar/get-avatar-portrait/{id}/{providerType}/{setGlobally} # Get portrait with provider
GET    /api/avatar/get-avatar-portrait-by-username/{username} # Get portrait by username
GET    /api/avatar/get-avatar-portrait-by-username/{username}/{providerType}/{setGlobally} # Get portrait by username with provider
GET    /api/avatar/get-avatar-portrait-by-email/{email} # Get portrait by email
GET    /api/avatar/get-avatar-portrait-by-email/{email}/{providerType}/{setGlobally} # Get portrait by email with provider
POST   /api/avatar/upload-avatar-portrait              # Upload avatar portrait
POST   /api/avatar/upload-avatar-portrait/{providerType}/{setGlobally} # Upload portrait with provider

# Avatar Details & Information
GET    /api/avatar/get-avatar-detail-by-id/{id}        # Get avatar details by ID
GET    /api/avatar/get-avatar-detail-by-id/{id}/{providerType}/{setGlobally} # Get details with provider
GET    /api/avatar/get-avatar-detail-by-email/{email} # Get avatar details by email
GET    /api/avatar/get-avatar-detail-by-email/{email}/{providerType}/{setGlobally} # Get details by email with provider
GET    /api/avatar/get-avatar-detail-by-username/{username} # Get avatar details by username
GET    /api/avatar/get-avatar-detail-by-username/{username}/{providerType}/{setGlobally} # Get details by username with provider
GET    /api/avatar/get-all-avatar-details              # Get all avatar details
GET    /api/avatar/get-all-avatar-details/{providerType}/{setGlobally} # Get all details with provider
GET    /api/avatar/get-all-avatars                     # Get all avatars
GET    /api/avatar/get-all-avatars/{providerType}/{setGlobally} # Get all avatars with provider

# Avatar Names & Grouping
GET    /api/avatar/get-all-avatar-names/{includeUsernames}/{includeIds} # Get all avatar names
GET    /api/avatar/get-all-avatar-names/{includeUsernames}/{includeIds}/{providerType}/{setGlobally} # Get names with provider
GET    /api/avatar/get-all-avatar-names-grouped-by-name/{includeUsernames}/{includeIds} # Get grouped names
GET    /api/avatar/get-all-avatar-names-grouped-by-name/{includeUsernames}/{includeIds}/{providerType}/{setGlobally} # Get grouped names with provider
```
GET    /api/avatar/provider-keys/{id}                 # Get avatar provider keys
POST   /api/avatar/update-avatar-detail               # Update avatar details
POST   /api/avatar/update-avatar-email                 # Update avatar email
POST   /api/avatar/update-avatar-username              # Update avatar username
POST   /api/avatar/update-avatar-password              # Update avatar password
POST   /api/avatar/forgot-password                     # Request password reset
POST   /api/avatar/reset-password                      # Reset password
POST   /api/avatar/verify-email                        # Verify email address
POST   /api/avatar/resend-verification                 # Resend email verification
```

### **Data Controller**
```http
GET    /api/data/load-holon/{id}                       # Load holon by ID
POST   /api/data/save-holon                           # Save holon
PUT    /api/data/update-holon/{id}                    # Update holon
DELETE /api/data/delete-holon/{id}                    # Delete holon
GET    /api/data/load-holons-for-parent/{parentId}    # Load holons for parent
GET    /api/data/load-all-holons                      # Load all holons
POST   /api/data/search-holons                        # Search holons
GET    /api/data/load-holon-by-meta-data              # Load holon by metadata
POST   /api/data/save-holon-with-options               # Save holon with options
```

### **Karma Controller**
```http
GET    /api/karma/load-karma-for-avatar/{avatarId}  # Load karma for avatar
POST   /api/karma/add-karma-to-avatar                 # Add karma to avatar
POST   /api/karma/remove-karma-from-avatar            # Remove karma from avatar
GET    /api/karma/get-karma-balance/{avatarId}        # Get karma balance
GET    /api/karma/get-karma-history/{avatarId}        # Get karma history
POST   /api/karma/transfer-karma                      # Transfer karma between avatars
GET    /api/karma/get-karma-leaderboard               # Get karma leaderboard
```

### **NFT Controller**
```http
GET    /api/nft/load-nft-by-id/{id}                                    # Get NFT by ID
GET    /api/nft/load-nft-by-hash/{hash}                               # Get NFT by hash
GET    /api/nft/load-all-nfts-for_avatar/{avatarId}                   # Get all NFTs for avatar
GET    /api/nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress} # Get NFTs for mint address
GET    /api/nft/load-all-geo-nfts-for-avatar/{avatarId}              # Get all GeoNFTs for avatar
GET    /api/nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress} # Get GeoNFTs for mint address
GET    /api/nft/load-all-nfts                                         # Get all NFTs
GET    /api/nft/load-all-geo-nfts                                     # Get all GeoNFTs
POST   /api/nft/send-nft                                              # Send NFT
POST   /api/nft/mint-nft                                              # Mint NFT
POST   /api/nft/place-geo-nft                                         # Place GeoNFT
POST   /api/nft/mint-and-place-geo-nft                                # Mint and place GeoNFT
GET    /api/nft/get-nft-provider-from-provider-type/{providerType}    # Get NFT provider
```

### **Search Controller**
```http
GET    /api/search                                    # Basic search
GET    /api/search/advanced                           # Advanced search
POST   /api/search/search-holons                      # Search holons
POST   /api/search/search-avatars                    # Search avatars
POST   /api/search/search-nfts                        # Search NFTs
POST   /api/search/search-files                       # Search files
```

### **Wallet Controller**
```http
POST   /api/wallet/send-token                         # Send token
GET    /api/wallet/balance                            # Get wallet balance
GET    /api/wallet/transactions                       # Get wallet transactions
GET    /api/wallet/addresses                          # Get wallet addresses
POST   /api/wallet/create-wallet                      # Create wallet
POST   /api/wallet/import-wallet                      # Import wallet
POST   /api/wallet/export-wallet                      # Export wallet
```

### **Keys Controller**
```http
POST   /api/keys/link-telos-account                    # Link Telos account
POST   /api/keys/link-eosio-account                   # Link EOSIO account
POST   /api/keys/link-holochain-agent                 # Link Holochain agent
GET    /api/keys/provider-key/{avatarUsername}/{providerType} # Get provider key
POST   /api/keys/generate-key-pair                    # Generate key pair
POST   /api/keys/import-key                           # Import key
POST   /api/keys/export-key                           # Export key
DELETE /api/keys/delete-key/{keyId}                   # Delete key
```

### **OLand Controller**
```http
GET    /api/oland/price/{count}                       # Get OLand price
POST   /api/oland/purchase                            # Purchase OLand
GET    /api/oland/load-all                            # Get all OLand
GET    /api/oland/{olandId}                           # Get OLand by ID
DELETE /api/oland/{avatarId}/{olandId}                # Delete OLand
POST   /api/oland/save                                # Save OLand
PUT    /api/oland/update                              # Update OLand
GET    /api/oland/avatar-oland/{avatarId}             # Get OLand for avatar
```

### **Files Controller**
```http
GET    /api/files/avatar-files                        # Get files for avatar
POST   /api/files/upload                              # Upload file
GET    /api/files/download/{fileId}                   # Download file
DELETE /api/files/{fileId}                            # Delete file
GET    /api/files/file-info/{fileId}                   # Get file info
POST   /api/files/share-file                          # Share file
GET    /api/files/shared-files                        # Get shared files
```

### **Chat Controller**
```http
POST   /api/chat/send                                 # Send message
GET    /api/chat/messages                             # Get messages
GET    /api/chat/rooms                                # Get chat rooms
POST   /api/chat/rooms                                # Create chat room
DELETE /api/chat/rooms/{roomId}                       # Delete chat room
POST   /api/chat/join-room                            # Join chat room
POST   /api/chat/leave-room                           # Leave chat room
```

### **Messaging Controller**
```http
POST   /api/messaging/send                            # Send message
GET    /api/messaging/messages                        # Get messages
GET    /api/messaging/conversations                   # Get conversations
POST   /api/messaging/start-conversation              # Start conversation
DELETE /api/messaging/conversation/{conversationId}   # Delete conversation
POST   /api/messaging/mark-read                       # Mark message as read
```

### **Social Controller**
```http
GET    /api/social/friends                            # Get friends
POST   /api/social/friends                            # Add friend
DELETE /api/social/friends/{friendId}                # Remove friend
GET    /api/social/followers                          # Get followers
POST   /api/social/follow                             # Follow user
DELETE /api/social/follow/{userId}                    # Unfollow user
GET    /api/social/following                          # Get following
POST   /api/social/block                              # Block user
DELETE /api/social/block/{userId}                    # Unblock user
```

### **Share Controller**
```http
POST   /api/share/content                             # Share content
GET    /api/share/content                             # Get shared content
GET    /api/share/stats                               # Get share statistics
POST   /api/share/like                                # Like content
POST   /api/share/unlike                              # Unlike content
POST   /api/share/comment                             # Comment on content
GET    /api/share/comments/{contentId}                # Get comments
```

### **Settings Controller**
```http
GET    /api/settings/user                             # Get user settings
PUT    /api/settings/user                             # Update user settings
GET    /api/settings/privacy                          # Get privacy settings
PUT    /api/settings/privacy                          # Update privacy settings
GET    /api/settings/notifications                    # Get notification settings
PUT    /api/settings/notifications                    # Update notification settings
```

### **Seeds Controller**
```http
GET    /api/seeds/balance                              # Get Seeds balance
GET    /api/seeds/transactions                        # Get Seeds transactions
POST   /api/seeds/transfer                            # Transfer Seeds
GET    /api/seeds/earn-opportunities                  # Get earn opportunities
POST   /api/seeds/claim-reward                        # Claim reward
GET    /api/seeds/leaderboard                         # Get Seeds leaderboard
```

### **Stats Controller**
```http
GET    /api/stats/user                                # Get user statistics
GET    /api/stats/system                              # Get system statistics
GET    /api/stats/provider                            # Get provider statistics
GET    /api/stats/network                             # Get network statistics
GET    /api/stats/performance                         # Get performance statistics
```

### **Video Controller**
```http
POST   /api/video/upload                              # Upload video
GET    /api/video/{videoId}                           # Get video
GET    /api/video/user-videos                         # Get user videos
DELETE /api/video/{videoId}                           # Delete video
POST   /api/video/stream                              # Stream video
GET    /api/video/thumbnail/{videoId}                  # Get video thumbnail
```

### **Solana Controller**
```http
POST   /api/solana/mint                               # Mint NFT
POST   /api/solana/send                               # Send transaction
GET    /api/solana/balance                            # Get balance
GET    /api/solana/account                            # Get account info
POST   /api/solana/create-account                     # Create account
POST   /api/solana/import-account                     # Import account
```

### **Telos Controller**
```http
GET    /api/telos/account                             # Get Telos account
GET    /api/telos/balance                             # Get Telos balance
POST   /api/telos/send                                # Send Telos transaction
GET    /api/telos/account-info                        # Get account information
POST   /api/telos/create-account                      # Create account
POST   /api/telos/import-account                      # Import account
```

### **Holochain Controller**
```http
GET    /api/holochain/agent                           # Get Holochain agent
GET    /api/holochain/data                            # Get Holochain data
POST   /api/holochain/store                           # Store Holochain data
GET    /api/holochain/entries                         # Get entries
POST   /api/holochain/create-entry                    # Create entry
POST   /api/holochain/update-entry                    # Update entry
```

### **ONODE Controller**
```http
GET    /api/onode/status                              # Get ONODE status
GET    /api/onode/config                              # Get ONODE configuration
GET    /api/onode/providers                           # Get provider information
GET    /api/onode/stats                               # Get ONODE statistics
```

### **ONET Controller**
```http
GET    /api/onet/status                               # Get ONET network status
GET    /api/onet/topology                             # Get network topology
GET    /api/onet/nodes                                # Get connected nodes
POST   /api/onet/connect                              # Connect to ONET node
POST   /api/onet/disconnect                           # Disconnect from ONET node
```

### **Settings Controller**
```http
GET    /api/settings                                  # Get system settings
PUT    /api/settings                                  # Update system settings
```

### **Map Controller**
```http
GET    /api/map/load-map/{mapId}                      # Load map
POST   /api/map/save-map                              # Save map
GET    /api/map/search-maps                           # Search maps
POST   /api/map/update-map                            # Update map
DELETE /api/map/delete-map/{mapId}                    # Delete map
```

### **Chat Controller**
```http
GET    /api/chat/conversations/{avatarId}             # Get conversations
POST   /api/chat/send-message                         # Send message
GET    /api/chat/messages/{conversationId}            # Get messages
POST   /api/chat/create-conversation                 # Create conversation
```

### **Messaging Controller**
```http
GET    /api/messaging/inbox/{avatarId}                # Get inbox
POST   /api/messaging/send-message                    # Send message
GET    /api/messaging/sent/{avatarId}                 # Get sent messages
POST   /api/messaging/mark-read                       # Mark message as read
```

### **Social Controller**
```http
GET    /api/social/friends/{avatarId}                 # Get friends
POST   /api/social/add-friend                         # Add friend
POST   /api/social/remove-friend                      # Remove friend
GET    /api/social/followers/{avatarId}                # Get followers
POST   /api/social/follow                             # Follow user
POST   /api/social/unfollow                           # Unfollow user
GET    /api/social/feed/{avatarId}                    # Get social feed
```

### **Files Controller**
```http
POST   /api/files/upload                              # Upload file
GET    /api/files/download/{fileId}                   # Download file
GET    /api/files/list/{avatarId}                     # List user files
DELETE /api/files/delete/{fileId}                     # Delete file
POST   /api/files/share                               # Share file
```

### **Keys Controller**
```http
GET    /api/keys/load-all-keys/{avatarId}             # Get all keys for avatar
POST   /api/keys/generate-key                         # Generate new key
POST   /api/keys/import-key                           # Import key
POST   /api/keys/export-key                           # Export key
DELETE /api/keys/delete-key/{keyId}                   # Delete key
POST   /api/keys/backup-keys                          # Backup keys
POST   /api/keys/restore-keys                         # Restore keys
```

### **Provider Controller**
```http
GET    /api/provider/active-providers                 # Get active providers
GET    /api/provider/available-providers              # Get available providers
POST   /api/provider/switch-provider                  # Switch provider
GET    /api/provider/provider-stats                   # Get provider statistics
POST   /api/provider/test-connection                  # Test provider connection
```

### **HyperDrive Controller**
```http
GET    /api/hyperdrive/status                         # Get HyperDrive status
GET    /api/hyperdrive/mode                           # Get HyperDrive mode
PUT    /api/hyperdrive/mode                           # Set HyperDrive mode
GET    /api/hyperdrive/providers                      # Get HyperDrive providers
POST   /api/hyperdrive/optimize                       # Optimize HyperDrive
GET    /api/hyperdrive/analytics                      # Get HyperDrive analytics
```

### **Subscription Controller**
```http
GET    /api/subscription/plans                        # Get subscription plans
POST   /api/subscription/subscribe                  # Subscribe to plan
GET    /api/subscription/status/{avatarId}             # Get subscription status
POST   /api/subscription/cancel                       # Cancel subscription
POST   /api/subscription/upgrade                      # Upgrade subscription
```

### **OLand Controller**
```http
GET    /api/oland/get-oland-price                     # Get OLand price
POST   /api/oland/purchase-oland                      # Purchase OLand
GET    /api/oland/load-all-olands                      # Get all OLand
GET    /api/oland/load-oland/{olandId}                 # Get specific OLand
POST   /api/oland/delete-oland/{olandId}               # Delete OLand
POST   /api/oland/save-oland                           # Save OLand
POST   /api/oland/update-oland                         # Update OLand
```

### **Competition Controller**
```http
GET    /api/competition/leaderboard                   # Get competition leaderboard
POST   /api/competition/join                          # Join competition
GET    /api/competition/status/{competitionId}        # Get competition status
POST   /api/competition/submit-score                  # Submit score
```

### **Gifts Controller**
```http
GET    /api/gifts/sent/{avatarId}                     # Get sent gifts
GET    /api/gifts/received/{avatarId}                 # Get received gifts
POST   /api/gifts/send-gift                           # Send gift
POST   /api/gifts/accept-gift                         # Accept gift
```

### **Eggs Controller**
```http
GET    /api/eggs/load-eggs/{avatarId}                 # Get avatar eggs
POST   /api/eggs/hatch-egg                            # Hatch egg
POST   /api/eggs/feed-egg                             # Feed egg
GET    /api/eggs/egg-stats/{eggId}                    # Get egg statistics
```

### **Seeds Controller**
```http
GET    /api/seeds/load-seeds/{avatarId}               # Get avatar seeds
POST   /api/seeds/plant-seed                          # Plant seed
POST   /api/seeds/water-seed                          # Water seed
GET    /api/seeds/seed-growth/{seedId}                # Get seed growth status
```

### **Share Controller**
```http
POST   /api/share/share-content                       # Share content
GET    /api/share/shared-content/{avatarId}           # Get shared content
POST   /api/share/like-content                        # Like content
POST   /api/share/comment-content                     # Comment on content
```

### **EOSIO Controller**
```http
GET    /api/eosio/account                             # Get EOSIO account
POST   /api/eosio/transaction                         # Send EOSIO transaction
GET    /api/eosio/balance                             # Get EOSIO balance
```

### **Data Controller**
```http
GET    /api/data/load-data/{id}                       # Load data by ID
POST   /api/data/save-data                            # Save data
PUT    /api/data/update-data/{id}                     # Update data
DELETE /api/data/delete-data/{id}                     # Delete data
GET    /api/data/search-data                          # Search data
POST   /api/data/upload-data                          # Upload data
GET    /api/data/download-data/{id}                   # Download data
POST   /api/data/share-data                           # Share data
```

### **EOSIO Controller**
```http
GET    /api/eosio/account                             # Get EOSIO account
GET    /api/eosio/balance                             # Get EOSIO balance
POST   /api/eosio/send                                # Send EOSIO transaction
GET    /api/eosio/account-info                        # Get account information
POST   /api/eosio/create-account                      # Create account
POST   /api/eosio/import-account                      # Import account
```

### **Mission Controller**
```http
GET    /api/mission/missions                          # Get all missions
GET    /api/mission/{missionId}                      # Get mission by ID
POST   /api/mission/create                            # Create mission
PUT    /api/mission/{missionId}                      # Update mission
DELETE /api/mission/{missionId}                      # Delete mission
POST   /api/mission/complete                         # Complete mission
GET    /api/mission/avatar-missions/{avatarId}       # Get missions for avatar
```

### **Quest Controller**
```http
GET    /api/quest/quests                              # Get all quests
GET    /api/quest/{questId}                           # Get quest by ID
POST   /api/quest/create                              # Create quest
PUT    /api/quest/{questId}                           # Update quest
DELETE /api/quest/{questId}                          # Delete quest
POST   /api/quest/complete                            # Complete quest
GET    /api/quest/avatar-quests/{avatarId}            # Get quests for avatar
```

### **Map Controller**
```http
GET    /api/map/data                                  # Get map data
PUT    /api/map/data                                  # Update map data
GET    /api/map/layers                                # Get map layers
POST   /api/map/create-layer                         # Create map layer
PUT    /api/map/layer/{layerId}                       # Update map layer
DELETE /api/map/layer/{layerId}                       # Delete map layer
```

### **OAPP Controller**
```http
GET    /api/oapp/oapps                                # Get all OAPPs
GET    /api/oapp/{oappId}                             # Get OAPP by ID
POST   /api/oapp/create                               # Create OAPP
PUT    /api/oapp/{oappId}                             # Update OAPP
DELETE /api/oapp/{oappId}                             # Delete OAPP
POST   /api/oapp/install                              # Install OAPP
POST   /api/oapp/uninstall                            # Uninstall OAPP
```

### **Cargo Controller**
```http
GET    /api/cargo/orders                              # Get cargo orders
POST   /api/cargo/orders                              # Create cargo order
PUT    /api/cargo/orders/{orderId}                    # Update cargo order
DELETE /api/cargo/orders/{orderId}                    # Delete cargo order
GET    /api/cargo/avatar-orders/{avatarId}            # Get orders for avatar
POST   /api/cargo/fulfill-order                       # Fulfill order
```

### **Gifts Controller**
```http
GET    /api/gifts/all                                 # Get all gifts
POST   /api/gifts/send                                # Send gift
POST   /api/gifts/receive                             # Receive gift
GET    /api/gifts/avatar-gifts/{avatarId}             # Get gifts for avatar
POST   /api/gifts/claim                               # Claim gift
```

### **Eggs Controller**
```http
GET    /api/eggs/all                                  # Get all eggs
GET    /api/eggs/quests                               # Get current egg quests
GET    /api/eggs/leaderboard                          # Get egg quest leaderboard
POST   /api/eggs/hatch                                # Hatch egg
GET    /api/eggs/avatar-eggs/{avatarId}               # Get eggs for avatar
```

### **Provider Controller**
```http
GET    /api/provider/providers                        # Get all providers
POST   /api/provider/register                         # Register provider
PUT    /api/provider/{providerId}                      # Update provider
DELETE /api/provider/{providerId}                     # Delete provider
GET    /api/provider/status/{providerId}              # Get provider status
POST   /api/provider/test-connection                  # Test provider connection
```

## 🔧 **Common Operations**

### **CRUD Operations**
All controllers support the standard CRUD operations:
- **Create**: `POST /api/{controller}`
- **Read**: `GET /api/{controller}` and `GET /api/{controller}/{id}`
- **Update**: `PUT /api/{controller}/{id}`
- **Delete**: `DELETE /api/{controller}/{id}`

### **Provider Operations**
All endpoints support provider-specific operations:
- **Provider Selection**: Specify `providerType` parameter
- **Global Setting**: Use `setGlobally` parameter
- **Auto-Failover**: Automatic provider switching

### **Authentication Operations**
- **Avatar Authentication**: All endpoints support avatar-based authentication
- **API Key Authentication**: All endpoints support API key authentication
- **Token Refresh**: Automatic token refresh for expired sessions

## 📊 **Response Format**

All endpoints return responses in the following format:

```json
{
  "result": "object|array",
  "isError": false,
  "message": "Success message",
  "exception": null
}
```

## 🔐 **Error Handling**

All endpoints include comprehensive error handling:
- **400 Bad Request**: Invalid request data
- **401 Unauthorized**: Authentication required
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Server error

## 🎯 **Provider Support**

### **Supported Providers**
- **Blockchain**: Ethereum, Solana, Telos, EOSIO, Holochain
- **Database**: MongoDB, PostgreSQL, SQL Server, MySQL
- **Storage**: IPFS, AWS S3, Azure Blob, Google Cloud Storage
- **Cloud**: AWS, Azure, Google Cloud Platform

### **Auto-Failover**
- **Intelligent Switching**: Automatically switches between providers
- **Performance Monitoring**: Real-time performance tracking
- **Cost Optimization**: Selects most cost-effective provider
- **Geographic Optimization**: Routes to nearest provider

## 📱 **SDKs**

### **JavaScript/Node.js**
```bash
npm install @oasis/api-client
```

### **C#/.NET**
```bash
dotnet add package OASIS.API.Client
```

### **Python**
```bash
pip install oasis-api-client
```

## 🚀 **Getting Started**

1. **Get API Key**: Sign up for an OASIS API key
2. **Choose SDK**: Select your preferred SDK
3. **Initialize Client**: Set up the API client
4. **Start Building**: Use the endpoints to create amazing applications

## 📞 **Support**

- **Documentation**: [docs.oasisplatform.world](https://docs.oasisplatform.world)
- **Community**: [Discord](https://discord.gg/oasis)
- **GitHub**: [github.com/oasisplatform](https://github.com/oasisplatform)
- **Email**: support@oasisplatform.world

---

*This is the complete reference for all WEB4 OASIS API endpoints. For detailed documentation of each endpoint, see the individual controller documentation.*
