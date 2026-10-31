module oasis::oasis {
    use std::signer;
    use std::vector;
    use aptos_std::table::{Self, Table};

    const E_NOT_INITIALIZED: u64 = 1;
    const E_ALREADY_INITIALIZED: u64 = 2;
    const E_RECORD_NOT_FOUND: u64 = 3;

    struct OasisStorage has key {
        records: Table<vector<u8>, vector<u8>>,
        keys: vector<vector<u8>>,
    }

    public entry fun initialize(account: &signer) {
        let owner = signer::address_of(account);
        assert!(!exists<OasisStorage>(owner), E_ALREADY_INITIALIZED);
        move_to(account, OasisStorage {
            records: table::new<vector<u8>, vector<u8>>(),
            keys: vector::empty<vector<u8>>(),
        });
    }

    public entry fun upsert_record(account: &signer, record_type: vector<u8>, provider_key: vector<u8>, payload: vector<u8>) acquires OasisStorage {
        let owner = signer::address_of(account);
        assert!(exists<OasisStorage>(owner), E_NOT_INITIALIZED);
        let key = composite_key(record_type, provider_key);
        let storage = borrow_global_mut<OasisStorage>(owner);
        if (table::contains(&storage.records, key)) {
            *table::borrow_mut(&mut storage.records, key) = payload;
        } else {
            vector::push_back(&mut storage.keys, key);
            table::add(&mut storage.records, key, payload);
        };
    }

    public entry fun delete_record(account: &signer, record_type: vector<u8>, provider_key: vector<u8>) acquires OasisStorage {
        let owner = signer::address_of(account);
        assert!(exists<OasisStorage>(owner), E_NOT_INITIALIZED);
        let key = composite_key(record_type, provider_key);
        let storage = borrow_global_mut<OasisStorage>(owner);
        assert!(table::contains(&storage.records, key), E_RECORD_NOT_FOUND);
        table::remove(&mut storage.records, key);
        let index = 0;
        let length = vector::length(&storage.keys);
        while (index < length) {
            if (*vector::borrow(&storage.keys, index) == key) {
                vector::swap_remove(&mut storage.keys, index);
                return
            };
            index = index + 1;
        };
    }

    #[view]
    public fun has_record(owner: address, record_type: vector<u8>, provider_key: vector<u8>): bool acquires OasisStorage {
        if (!exists<OasisStorage>(owner)) return false;
        table::contains(&borrow_global<OasisStorage>(owner).records, composite_key(record_type, provider_key))
    }

    #[view]
    public fun get_record(owner: address, record_type: vector<u8>, provider_key: vector<u8>): vector<u8> acquires OasisStorage {
        assert!(exists<OasisStorage>(owner), E_NOT_INITIALIZED);
        let key = composite_key(record_type, provider_key);
        let storage = borrow_global<OasisStorage>(owner);
        assert!(table::contains(&storage.records, key), E_RECORD_NOT_FOUND);
        *table::borrow(&storage.records, key)
    }

    #[view]
    public fun get_record_keys(owner: address): vector<vector<u8>> acquires OasisStorage {
        assert!(exists<OasisStorage>(owner), E_NOT_INITIALIZED);
        borrow_global<OasisStorage>(owner).keys
    }

    fun composite_key(record_type: vector<u8>, provider_key: vector<u8>): vector<u8> {
        vector::push_back(&mut record_type, 0);
        vector::append(&mut record_type, provider_key);
        record_type
    }

    #[test_only]
    use aptos_framework::account;

    #[test]
    fun record_crud_uses_durable_table_storage() acquires OasisStorage {
        let owner = account::create_account_for_test(@oasis);
        initialize(&owner);
        upsert_record(&owner, b"holon", b"one", b"first");
        assert!(has_record(@oasis, b"holon", b"one"), 10);
        assert!(get_record(@oasis, b"holon", b"one") == b"first", 11);

        upsert_record(&owner, b"holon", b"one", b"updated");
        assert!(get_record(@oasis, b"holon", b"one") == b"updated", 12);
        assert!(vector::length(&get_record_keys(@oasis)) == 1, 13);

        delete_record(&owner, b"holon", b"one");
        assert!(!has_record(@oasis, b"holon", b"one"), 14);
        assert!(vector::is_empty(&get_record_keys(@oasis)), 15);
    }
}
