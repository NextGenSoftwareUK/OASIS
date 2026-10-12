//! OASIS Storage component for Radix.
//!
//! Stores OASIS Avatars, AvatarDetails and Holons as JSON, keyed by their OASIS GUID (as a string), with
//! indexes for username, email and provider key. Used by RadixOASIS when `OasisBlueprintAddress` is set.
//!
//! Reads are public. Every write needs a proof of the owner badge minted by `instantiate`, which the deployer
//! deposits into the provider's technical account.

use scrypto::prelude::*;

#[blueprint]
mod oasis_storage {
    enable_method_auth! {
        roles {
            writer => updatable_by: [OWNER];
        },
        methods {
            save_avatar => restrict_to: [writer, OWNER];
            delete_avatar => restrict_to: [writer, OWNER];
            save_avatar_detail => restrict_to: [writer, OWNER];
            delete_avatar_detail => restrict_to: [writer, OWNER];
            save_holon => restrict_to: [writer, OWNER];
            delete_holon => restrict_to: [writer, OWNER];
            get_avatar => PUBLIC;
            get_avatar_by_username => PUBLIC;
            get_avatar_by_email => PUBLIC;
            get_avatar_detail => PUBLIC;
            get_holon => PUBLIC;
            get_holon_by_provider_key => PUBLIC;
            get_avatar_count => PUBLIC;
            get_avatar_detail_count => PUBLIC;
            get_holon_count => PUBLIC;
        }
    }

    struct OasisStorage {
        /// OASIS id (GUID string) -> avatar JSON
        avatars: KeyValueStore<String, String>,
        /// OASIS id -> avatar detail JSON
        avatar_details: KeyValueStore<String, String>,
        /// OASIS id -> holon JSON
        holons: KeyValueStore<String, String>,

        /// Lookup indexes: value -> OASIS id
        username_to_avatar_id: KeyValueStore<String, String>,
        email_to_avatar_id: KeyValueStore<String, String>,
        provider_key_to_holon_id: KeyValueStore<String, String>,

        /// Reverse indexes, so a save or delete can drop stale lookup entries: OASIS id -> value
        avatar_username: KeyValueStore<String, String>,
        avatar_email: KeyValueStore<String, String>,
        holon_provider_key: KeyValueStore<String, String>,

        avatar_count: u64,
        avatar_detail_count: u64,
        holon_count: u64,
    }

    impl OasisStorage {
        /// Creates the component and returns it with its owner badge. Keep the badge in the RadixOASIS
        /// technical account: writes must present a proof of it.
        pub fn instantiate() -> (Global<OasisStorage>, Bucket) {
            let owner_badge: Bucket = ResourceBuilder::new_fungible(OwnerRole::None)
                .divisibility(DIVISIBILITY_NONE)
                .metadata(metadata! {
                    init {
                        "name" => "OASIS Storage owner badge", locked;
                    }
                })
                .mint_initial_supply(1)
                .into();

            let owner_rule = rule!(require(owner_badge.resource_address()));

            let component = Self {
                avatars: KeyValueStore::new(),
                avatar_details: KeyValueStore::new(),
                holons: KeyValueStore::new(),
                username_to_avatar_id: KeyValueStore::new(),
                email_to_avatar_id: KeyValueStore::new(),
                provider_key_to_holon_id: KeyValueStore::new(),
                avatar_username: KeyValueStore::new(),
                avatar_email: KeyValueStore::new(),
                holon_provider_key: KeyValueStore::new(),
                avatar_count: 0,
                avatar_detail_count: 0,
                holon_count: 0,
            }
            .instantiate()
            .prepare_to_globalize(OwnerRole::Fixed(owner_rule.clone()))
            .roles(roles! {
                writer => owner_rule;
            })
            .globalize();

            (component, owner_badge)
        }

        // ---------------------------------------------------------------- avatars

        /// Creates or replaces an avatar and its username/email indexes. Empty username/email are not indexed.
        pub fn save_avatar(&mut self, id: String, avatar_json: String, username: String, email: String) {
            Self::assert_id(&id);
            Self::claim_index(&self.username_to_avatar_id, &username, &id, "username");
            Self::claim_index(&self.email_to_avatar_id, &email, &id, "email");

            if self.avatars.get(&id).is_none() {
                self.avatar_count += 1;
            }
            self.avatars.insert(id.clone(), avatar_json);

            Self::reindex(&self.username_to_avatar_id, &self.avatar_username, &id, username);
            Self::reindex(&self.email_to_avatar_id, &self.avatar_email, &id, email);
        }

        /// Removes an avatar and its indexes. Returns false if it didn't exist.
        pub fn delete_avatar(&mut self, id: String) -> bool {
            if self.avatars.remove(&id).is_none() {
                return false;
            }
            self.avatar_count -= 1;
            Self::reindex(&self.username_to_avatar_id, &self.avatar_username, &id, String::new());
            Self::reindex(&self.email_to_avatar_id, &self.avatar_email, &id, String::new());
            true
        }

        pub fn get_avatar(&self, id: String) -> Option<String> {
            self.avatars.get(&id).map(|json| json.clone())
        }

        pub fn get_avatar_by_username(&self, username: String) -> Option<String> {
            let id = self.username_to_avatar_id.get(&username).map(|id| id.clone())?;
            self.get_avatar(id)
        }

        pub fn get_avatar_by_email(&self, email: String) -> Option<String> {
            let id = self.email_to_avatar_id.get(&email).map(|id| id.clone())?;
            self.get_avatar(id)
        }

        // --------------------------------------------------------- avatar details

        pub fn save_avatar_detail(&mut self, id: String, avatar_detail_json: String) {
            Self::assert_id(&id);
            if self.avatar_details.get(&id).is_none() {
                self.avatar_detail_count += 1;
            }
            self.avatar_details.insert(id, avatar_detail_json);
        }

        pub fn delete_avatar_detail(&mut self, id: String) -> bool {
            if self.avatar_details.remove(&id).is_none() {
                return false;
            }
            self.avatar_detail_count -= 1;
            true
        }

        pub fn get_avatar_detail(&self, id: String) -> Option<String> {
            self.avatar_details.get(&id).map(|json| json.clone())
        }

        // ----------------------------------------------------------------- holons

        /// Creates or replaces a holon. A non-empty provider key is indexed for get_holon_by_provider_key.
        pub fn save_holon(&mut self, id: String, holon_json: String, provider_key: String) {
            Self::assert_id(&id);
            Self::claim_index(&self.provider_key_to_holon_id, &provider_key, &id, "provider key");

            if self.holons.get(&id).is_none() {
                self.holon_count += 1;
            }
            self.holons.insert(id.clone(), holon_json);

            Self::reindex(&self.provider_key_to_holon_id, &self.holon_provider_key, &id, provider_key);
        }

        pub fn delete_holon(&mut self, id: String) -> bool {
            if self.holons.remove(&id).is_none() {
                return false;
            }
            self.holon_count -= 1;
            Self::reindex(&self.provider_key_to_holon_id, &self.holon_provider_key, &id, String::new());
            true
        }

        pub fn get_holon(&self, id: String) -> Option<String> {
            self.holons.get(&id).map(|json| json.clone())
        }

        pub fn get_holon_by_provider_key(&self, provider_key: String) -> Option<String> {
            let id = self.provider_key_to_holon_id.get(&provider_key).map(|id| id.clone())?;
            self.get_holon(id)
        }

        // ----------------------------------------------------------------- counts

        pub fn get_avatar_count(&self) -> u64 {
            self.avatar_count
        }

        pub fn get_avatar_detail_count(&self) -> u64 {
            self.avatar_detail_count
        }

        pub fn get_holon_count(&self) -> u64 {
            self.holon_count
        }

        // ---------------------------------------------------------------- helpers

        fn assert_id(id: &String) {
            assert!(!id.is_empty(), "id must not be empty");
        }

        /// Fails if `value` is already indexed to a different entity (e.g. a username taken by another avatar).
        fn claim_index(index: &KeyValueStore<String, String>, value: &String, id: &String, what: &str) {
            if value.is_empty() {
                return;
            }
            if let Some(owner) = index.get(value) {
                assert!(*owner == *id, "{} '{}' is already used by {}", what, value, *owner);
            }
        }

        /// Points `index[value] -> id`, removing the entity's previous value. An empty value just removes it.
        fn reindex(
            index: &KeyValueStore<String, String>,
            reverse: &KeyValueStore<String, String>,
            id: &String,
            value: String,
        ) {
            let previous = reverse.get(id).map(|v| v.clone());
            if let Some(previous) = previous {
                if previous != value {
                    index.remove(&previous);
                }
            }
            if value.is_empty() {
                reverse.remove(id);
            } else {
                index.insert(value.clone(), id.clone());
                reverse.insert(id.clone(), value);
            }
        }
    }
}
