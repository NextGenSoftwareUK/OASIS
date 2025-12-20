//! OASIS Storage Component for Radix
//! 
//! This Scrypto blueprint provides persistent storage for OASIS Avatars, AvatarDetails, and Holons
//! on the Radix blockchain. It mirrors the functionality of the ArbitrumOASIS Solidity smart contract.

use scrypto::prelude::*;

#[blueprint]
mod oasis_storage {
    /// OASIS Storage Component
    /// 
    /// Stores Avatars, AvatarDetails, and Holons as JSON strings in key-value stores.
    /// Uses entity IDs (u64) as keys for efficient lookups.
    struct OasisStorage {
        // Avatar storage: EntityId -> JSON string
        avatars: KeyValueStore<u64, String>,
        
        // AvatarDetail storage: EntityId -> JSON string
        avatar_details: KeyValueStore<u64, String>,
        
        // Holon storage: EntityId -> JSON string
        holons: KeyValueStore<u64, String>,
        
        // Indexes for lookups by username/email/provider_key
        username_to_avatar_id: KeyValueStore<String, u64>,
        email_to_avatar_id: KeyValueStore<String, u64>,
        provider_key_to_holon_id: KeyValueStore<String, u64>,
        
        // Counters
        avatar_count: u64,
        avatar_detail_count: u64,
        holon_count: u64,
    }

    impl OasisStorage {
        /// Creates a new OASIS Storage component
        pub fn instantiate() -> Global<OasisStorage> {
            Self {
                avatars: KeyValueStore::new(),
                avatar_details: KeyValueStore::new(),
                holons: KeyValueStore::new(),
                username_to_avatar_id: KeyValueStore::new(),
                email_to_avatar_id: KeyValueStore::new(),
                provider_key_to_holon_id: KeyValueStore::new(),
                avatar_count: 0,
                avatar_detail_count: 0,
                holon_count: 0,
            }
            .instantiate()
            .prepare_to_globalize(OwnerRole::None)
            .globalize()
        }

        /// Creates a new avatar with the given entity ID and JSON data
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID (hash of avatar GUID)
        /// * `avatar_json` - JSON string representation of the avatar
        /// * `username` - Avatar username for indexing
        /// * `email` - Avatar email for indexing
        /// 
        /// # Returns
        /// The entity ID of the created avatar
        pub fn create_avatar(
            &mut self,
            entity_id: u64,
            avatar_json: String,
            username: String,
            email: String,
        ) -> u64 {
            // Check if avatar already exists
            if self.avatars.get(&entity_id).is_some() {
                panic!("Avatar with entity_id {} already exists", entity_id);
            }

            // Check if username/email already indexed
            if self.username_to_avatar_id.get(&username).is_some() {
                panic!("Username {} already exists", username);
            }
            if self.email_to_avatar_id.get(&email).is_some() {
                panic!("Email {} already exists", email);
            }

            // Store avatar
            self.avatars.insert(entity_id, avatar_json);
            
            // Update indexes
            self.username_to_avatar_id.insert(username, entity_id);
            self.email_to_avatar_id.insert(email, entity_id);
            
            // Increment counter
            self.avatar_count += 1;

            entity_id
        }

        /// Updates an existing avatar
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID of the avatar to update
        /// * `avatar_json` - Updated JSON string representation of the avatar
        pub fn update_avatar(&mut self, entity_id: u64, avatar_json: String) {
            if self.avatars.get(&entity_id).is_none() {
                panic!("Avatar with entity_id {} does not exist", entity_id);
            }
            self.avatars.insert(entity_id, avatar_json);
        }

        /// Gets an avatar by entity ID
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID to look up
        /// 
        /// # Returns
        /// Option containing the avatar JSON string, or None if not found
        pub fn get_avatar(&self, entity_id: u64) -> Option<String> {
            self.avatars.get(&entity_id)
        }

        /// Gets an avatar by username
        /// 
        /// # Arguments
        /// * `username` - The username to look up
        /// 
        /// # Returns
        /// Option containing the avatar JSON string, or None if not found
        pub fn get_avatar_by_username(&self, username: String) -> Option<String> {
            if let Some(entity_id) = self.username_to_avatar_id.get(&username) {
                self.avatars.get(&entity_id)
            } else {
                None
            }
        }

        /// Gets an avatar by email
        /// 
        /// # Arguments
        /// * `email` - The email to look up
        /// 
        /// # Returns
        /// Option containing the avatar JSON string, or None if not found
        pub fn get_avatar_by_email(&self, email: String) -> Option<String> {
            if let Some(entity_id) = self.email_to_avatar_id.get(&email) {
                self.avatars.get(&entity_id)
            } else {
                None
            }
        }

        /// Deletes an avatar (soft delete by setting isActive flag in JSON, or remove from storage)
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID of the avatar to delete
        pub fn delete_avatar(&mut self, entity_id: u64) {
            // Remove from storage (hard delete)
            // For soft delete, would update JSON and set isActive: false
            if self.avatars.get(&entity_id).is_none() {
                panic!("Avatar with entity_id {} does not exist", entity_id);
            }
            self.avatars.remove(&entity_id);
        }

        /// Creates a new avatar detail with the given entity ID and JSON data
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID (hash of avatar detail GUID)
        /// * `avatar_detail_json` - JSON string representation of the avatar detail
        /// 
        /// # Returns
        /// The entity ID of the created avatar detail
        pub fn create_avatar_detail(
            &mut self,
            entity_id: u64,
            avatar_detail_json: String,
        ) -> u64 {
            if self.avatar_details.get(&entity_id).is_some() {
                panic!("AvatarDetail with entity_id {} already exists", entity_id);
            }

            self.avatar_details.insert(entity_id, avatar_detail_json);
            self.avatar_detail_count += 1;

            entity_id
        }

        /// Gets an avatar detail by entity ID
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID to look up
        /// 
        /// # Returns
        /// Option containing the avatar detail JSON string, or None if not found
        pub fn get_avatar_detail(&self, entity_id: u64) -> Option<String> {
            self.avatar_details.get(&entity_id)
        }

        /// Creates a new holon with the given entity ID and JSON data
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID (hash of holon GUID)
        /// * `holon_json` - JSON string representation of the holon
        /// * `provider_key` - Provider unique storage key for indexing
        /// 
        /// # Returns
        /// The entity ID of the created holon
        pub fn create_holon(
            &mut self,
            entity_id: u64,
            holon_json: String,
            provider_key: String,
        ) -> u64 {
            if self.holons.get(&entity_id).is_some() {
                panic!("Holon with entity_id {} already exists", entity_id);
            }

            self.holons.insert(entity_id, holon_json);
            
            if !provider_key.is_empty() {
                self.provider_key_to_holon_id.insert(provider_key, entity_id);
            }
            
            self.holon_count += 1;

            entity_id
        }

        /// Updates an existing holon
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID of the holon to update
        /// * `holon_json` - Updated JSON string representation of the holon
        pub fn update_holon(&mut self, entity_id: u64, holon_json: String) {
            if self.holons.get(&entity_id).is_none() {
                panic!("Holon with entity_id {} does not exist", entity_id);
            }
            self.holons.insert(entity_id, holon_json);
        }

        /// Gets a holon by entity ID
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID to look up
        /// 
        /// # Returns
        /// Option containing the holon JSON string, or None if not found
        pub fn get_holon(&self, entity_id: u64) -> Option<String> {
            self.holons.get(&entity_id)
        }

        /// Gets a holon by provider key
        /// 
        /// # Arguments
        /// * `provider_key` - The provider key to look up
        /// 
        /// # Returns
        /// Option containing the holon JSON string, or None if not found
        pub fn get_holon_by_provider_key(&self, provider_key: String) -> Option<String> {
            if let Some(entity_id) = self.provider_key_to_holon_id.get(&provider_key) {
                self.holons.get(&entity_id)
            } else {
                None
            }
        }

        /// Deletes a holon
        /// 
        /// # Arguments
        /// * `entity_id` - The entity ID of the holon to delete
        pub fn delete_holon(&mut self, entity_id: u64) {
            if self.holons.get(&entity_id).is_none() {
                panic!("Holon with entity_id {} does not exist", entity_id);
            }
            self.holons.remove(&entity_id);
        }

        /// Gets the total count of avatars
        pub fn get_avatar_count(&self) -> u64 {
            self.avatar_count
        }

        /// Gets the total count of avatar details
        pub fn get_avatar_detail_count(&self) -> u64 {
            self.avatar_detail_count
        }

        /// Gets the total count of holons
        pub fn get_holon_count(&self) -> u64 {
            self.holon_count
        }
    }
}

