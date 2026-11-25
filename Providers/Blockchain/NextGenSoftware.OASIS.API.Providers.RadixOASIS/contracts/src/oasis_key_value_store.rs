//! Generic OASIS key-value record store for Radix, used by RadixOASIS core storage.
//!
//! One component holds one `KeyValueStore<String, String>`. RadixOASIS keeps its records here through the shared
//! `KeyValueStorageProviderBase`; the values it writes are already encrypted and compressed, because ledger state is
//! public. Reads go through the node's Engine State API (`/kv-store/entry` and `/kv-store/iterator`), so the blueprint
//! needs no read methods beyond `get`, which exists for manifests and tests.
//!
//! Writes are restricted to the owner badge given at instantiation, normally the signature badge of the provider's
//! account key, so a write is authorised by the transaction signature alone.

use scrypto::prelude::*;

#[blueprint]
mod oasis_key_value_store {
    enable_method_auth! {
        methods {
            put => restrict_to: [OWNER];
            delete => restrict_to: [OWNER];
            get => PUBLIC;
        }
    }

    struct OasisKeyValueStore {
        entries: KeyValueStore<String, String>,
    }

    impl OasisKeyValueStore {
        /// Creates a store whose writes require `owner_badge`, normally the signature badge of the writer's key.
        pub fn instantiate(owner_badge: NonFungibleGlobalId) -> Global<OasisKeyValueStore> {
            Self { entries: KeyValueStore::new() }
                .instantiate()
                .prepare_to_globalize(OwnerRole::Fixed(rule!(require(owner_badge))))
                .globalize()
        }

        /// Creates or replaces the value of `key`.
        pub fn put(&mut self, key: String, value: String) {
            assert!(!key.is_empty(), "key must not be empty");
            self.entries.insert(key, value);
        }

        /// Removes `key`. Removing a missing key succeeds and returns false.
        pub fn delete(&mut self, key: String) -> bool {
            self.entries.remove(&key).is_some()
        }

        pub fn get(&self, key: String) -> Option<String> {
            self.entries.get(&key).map(|value| value.clone())
        }
    }
}
