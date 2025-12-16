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
GET    /api/karma/get-positive-karma-weighting/{karmaType}                      # Get positive karma weighting
GET    /api/karma/get-positive-karma-weighting/{karmaType}/{providerType}/{setGlobally}
GET    /api/karma/get-negative-karma-weighting/{karmaType}                      # Get negative karma weighting
GET    /api/karma/get-negative-karma-weighting/{karmaType}/{providerType}/{setGlobally}
GET    /api/karma/get-karma-for-avatar/{avatarId}                               # Get total karma for avatar
GET    /api/karma/get-karma-for-avatar/{avatarId}/{providerType}/{setGlobally}
GET    /api/karma/get-karma-akashic-records-for-avatar/{avatarId}              # Get karma Akashic records
POST   /api/karma/add-karma-to-avatar/{avatarId}                               # Add karma to avatar
POST   /api/karma/add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}
POST   /api/karma/remove-karma-from-avatar/{avatarId}                          # Remove karma from avatar
POST   /api/karma/remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}
GET    /api/karma/get-karma-stats/{avatarId}                                   # Get karma statistics
GET    /api/karma/get-karma-stats/{avatarId}/{providerType}/{setGlobally}
GET    /api/karma/get-karma-history/{avatarId}                                 # Get karma history
GET    /api/karma/get-karma-history/{avatarId}/{providerType}/{setGlobally}
```

### **NFT Controller**
```http
GET    /api/nft/load-nft-by-id/{id}                                             # Load Web4 NFT by ID
GET    /api/nft/load-nft-by-id/{id}/{providerType}/{setGlobally}
GET    /api/nft/load-nft-by-hash/{hash}                                        # Load Web4 NFT by hash
GET    /api/nft/load-nft-by-hash/{hash}/{providerType}/{setGlobally}
GET    /api/nft/load-all-nfts-for_avatar/{avatarId}                            # All Web4 NFTs for avatar
GET    /api/nft/load-all-nfts-for_avatar/{avatarId}/{providerType}/{setGlobally}
GET    /api/nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress}      # Web4 NFTs by mint address
GET    /api/nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}
GET    /api/nft/load-all-geo-nfts-for-avatar/{avatarId}                        # All GeoNFTs for avatar
GET    /api/nft/load-all-geo-nfts-for-avatar/{avatarId}/{providerType}/{setGlobally}
GET    /api/nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}  # GeoNFTs by mint address
GET    /api/nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}/{providerType}/{setGlobally}
GET    /api/nft/load-all-nfts                                                  # All Web4 NFTs (admin)
GET    /api/nft/load-all-nfts/{providerType}/{setGlobally}
GET    /api/nft/load-all-geo-nfts                                              # All GeoNFTs (admin)
GET    /api/nft/load-all-geo-nfts/{providerType}/{setGlobally}
POST   /api/nft/send-nft                                                       # Send NFT
POST   /api/nft/mint-nft                                                       # Mint Web4 NFT
POST   /api/nft/place-geo-nft                                                  # Place GeoNFT
POST   /api/nft/mint-and-place-geo-nft                                         # Mint and place GeoNFT
POST   /api/nft/remint-nft                                                     # Remint NFT
POST   /api/nft/import-web3-nft                                                # Import Web3 NFT
POST   /api/nft/import-web4-nft-from-file/{importedByAvatarId}/{fullPath}      # Import Web4 NFT from file
POST   /api/nft/import-web4-nft/{importedByAvatarId}                           # Import Web4 NFT object
POST   /api/nft/export-web4-nft-to-file/{oasisNFTId}/{fullPath}                # Export Web4 NFT to file
POST   /api/nft/export-web4-nft                                                # Export Web4 NFT object
GET    /api/nft/load-web3-nft-by-id/{id}                                       # Load Web3 NFT by ID
GET    /api/nft/load-web3-nft-by-hash/{onChainNftHash}                         # Load Web3 NFT by hash
GET    /api/nft/load-all-web3-nfts-for-avatar/{avatarId}                       # Web3 NFTs for avatar
GET    /api/nft/load-all-web3-nfts-for-mint-address/{mintWalletAddress}        # Web3 NFTs for mint address
GET    /api/nft/load-all-web3-nfts                                             # All Web3 NFTs (admin)
POST   /api/nft/create-web4-nft-collection                                     # Create NFT collection
GET    /api/nft/search-web4-nfts/{searchTerm}/{avatarId}                       # Search Web4 NFTs
GET    /api/nft/search-web4-geo-nfts/{searchTerm}/{avatarId}                   # Search GeoNFTs
GET    /api/nft/search-web4-nft-collections/{searchTerm}/{avatarId}            # Search NFT collections
GET    /api/nft/get-nft-provider-from-provider-type/{providerType}             # Get NFT provider
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
POST   /api/wallet/send_token                                         # Send Web4 token
GET    /api/wallet/avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}              # Provider wallets by ID
GET    /api/wallet/avatar/username/{username}/wallets/{showOnlyDefault}/{decryptPrivateKeys} # Provider wallets by username
GET    /api/wallet/avatar/email/{email}/wallets                       # Provider wallets by email
GET    /api/wallet/avatar/email/{email}/wallets/{showOnlyDefault}/{decryptPrivateKeys}/{providerType}
POST   /api/wallet/avatar/{id}/wallets                                # Save provider wallets by ID
POST   /api/wallet/avatar/username/{username}/wallets                 # Save provider wallets by username
POST   /api/wallet/avatar/email/{email}/wallets                       # Save provider wallets by email
GET    /api/wallet/avatar/{id}/default-wallet                         # Get default wallet by ID
GET    /api/wallet/avatar/username/{username}/default-wallet/{showOnlyDefault}/{decryptPrivateKeys} # Get default wallet by username
GET    /api/wallet/avatar/email/{email}/default-wallet                # Get default wallet by email
POST   /api/wallet/avatar/{id}/default-wallet/{walletId}              # Set default wallet by ID
POST   /api/wallet/avatar/username/{username}/default-wallet/{walletId} # Set default wallet by username
POST   /api/wallet/avatar/email/{email}/default-wallet/{walletId}     # Set default wallet by email
POST   /api/wallet/avatar/{avatarId}/import/private-key               # Import wallet using private key (ID)
POST   /api/wallet/avatar/username/{username}/import/private-key      # Import wallet using private key (username)
POST   /api/wallet/avatar/email/{email}/import/private-key            # Import wallet using private key (email)
POST   /api/wallet/avatar/{avatarId}/import/public-key                # Import wallet using public key (ID)
POST   /api/wallet/avatar/username/{username}/import/public-key       # Import wallet using public key (username)
POST   /api/wallet/avatar/email/{email}/import/public-key             # Import wallet using public key (email)
GET    /api/wallet/find-wallet                                        # Get wallet for public key
GET    /api/wallet/avatar/{avatarId}/portfolio/value                  # Portfolio value (demo)
GET    /api/wallet/avatar/{avatarId}/wallets/chain/{chain}            # Wallets by chain (demo)
POST   /api/wallet/transfer                                           # Transfer between wallets (demo)
GET    /api/wallet/avatar/{avatarId}/wallet/{walletId}/analytics      # Wallet analytics (demo)
GET    /api/wallet/supported-chains                                   # Supported chains (demo)
GET    /api/wallet/avatar/{avatarId}/wallet/{walletId}/tokens         # Wallet tokens (demo)
POST   /api/wallet/avatar/{avatarId}/create-wallet                    # Create wallet for avatar by ID
POST   /api/wallet/avatar/username/{username}/create-wallet           # Create wallet for avatar by username
POST   /api/wallet/avatar/email/{email}/create-wallet                 # Create wallet for avatar by email
PUT    /api/wallet/avatar/{avatarId}/wallet/{walletId}                # Update wallet for avatar by ID
PUT    /api/wallet/avatar/username/{username}/wallet/{walletId}       # Update wallet for avatar by username
PUT    /api/wallet/avatar/email/{email}/wallet/{walletId}             # Update wallet for avatar by email
```

### **Keys Controller**
```http
POST   /api/keys/clear_cache                                         # Clear key cache
POST   /api/keys/link_provider_public_key_to_avatar_by_id            # Link provider public key by ID
POST   /api/keys/link_provider_public_key_to_avatar_by_username      # Link provider public key by username
POST   /api/keys/link_provider_public_key_to_avatar_by_email         # Link provider public key by email
POST   /api/keys/link_provider_private_key_to_avatar_by_id           # Link provider private key by ID
POST   /api/keys/link_provider_private_key_to_avatar_by_username     # Link provider private key by username
POST   /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_id        # Generate keypair & link (ID)
POST   /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_username  # Generate keypair & link (username)
POST   /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_email     # Generate keypair & link (email)
POST   /api/keys/generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_id       # Keypair+wallet & link (ID)
POST   /api/keys/generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_username # Keypair+wallet & link (username)
POST   /api/keys/generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_email    # Keypair+wallet & link (email)
POST   /api/keys/generate_keypair_for_provider/{providerType}         # Generate keypair for provider
POST   /api/keys/generate_keypair/{keyPrefix}                        # Generate keypair with prefix
POST   /api/keys/generate_keypair_with_wallet_address_for_provider/{providerType} # Keypair+wallet for provider
GET    /api/keys/get_provider_unique_storage_key_for_avatar_by_id    # Get unique storage key (ID)
GET    /api/keys/get_provider_unique_storage_key_for_avatar_by_username # Get unique storage key (username)
GET    /api/keys/get_provider_unique_storage_key_for_avatar_by_email    # Get unique storage key (email)
GET    /api/keys/get_provider_private_key_for_avatar_by_id           # Get private keys (ID)
GET    /api/keys/get_provider_private_key_for_avatar_by_username     # Get private keys (username)
GET    /api/keys/get_provider_public_keys_for_avatar_by_id           # Get public keys (ID)
GET    /api/keys/get_provider_public_keys_for_avatar_by_username     # Get public keys (username)
GET    /api/keys/get_provider_public_keys_for_avatar_by_email        # Get public keys (email)
GET    /api/keys/get_all_provider_public_keys_for_avatar_by_id/{id}  # All public keys (ID)
GET    /api/keys/get_all_provider_public_keys_for_avatar_by_username/{username} # All public keys (username)
GET    /api/keys/get_all_provider_public_keys_for_avatar_by_email/{email}      # All public keys (email)
GET    /api/keys/get_all_provider_private_keys_for_avatar_by_id/{id} # All private keys (ID)
GET    /api/keys/get_all_provider_private_keys_for_avatar_by_username/{username} # All private keys (username)
GET    /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_id/{id}    # All storage keys (ID)
GET    /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_username/{username} # All storage keys (username)
GET    /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_email/{email}      # All storage keys (email)
GET    /api/keys/get_avatar_id_for_provider_unique_storage_key/{providerKey}   # Avatar by unique storage key
GET    /api/keys/get_avatar_username_for_provider_unique_storage_key/{providerKey}
GET    /api/keys/get_avatar_email_for_provider_unique_storage_key/{providerKey}
GET    /api/keys/get_avatar_for_provider_unique_storage_key/{providerKey}
GET    /api/keys/get_avatar_id_for_provider_public_key/{providerKey}           # Avatar by public key
GET    /api/keys/get_avatar_username_for_provider_public_key/{providerKey}
GET    /api/keys/get_avatar_email_for_provider_public_key/{providerKey}
GET    /api/keys/get_avatar_for_provider_public_key/{providerKey}
POST   /api/keys/get_private_wifi/{source}                                     # Get private WIF
POST   /api/keys/get_public_wifi                                              # Get public WIF
POST   /api/keys/decode_private_wif/{data}                                     # Decode private WIF
POST   /api/keys/base58_check_decode/{data}                                    # Base58 check decode
POST   /api/keys/encode_signature/{source}                                     # Encode signature
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
GET    /api/files/get-all-files-stored-for-current-logged-in-avatar    # Files for current avatar
POST   /api/files/upload-file                                          # Upload file
GET    /api/files/download-file/{fileId}                               # Download file
DELETE /api/files/delete-file/{fileId}                                 # Delete file
GET    /api/files/file-metadata/{fileId}                               # Get file metadata
PUT    /api/files/update-file-metadata/{fileId}                        # Update file metadata
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
