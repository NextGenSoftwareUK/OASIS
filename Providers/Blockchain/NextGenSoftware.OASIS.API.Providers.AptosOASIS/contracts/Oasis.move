module Oasis::oasis {
    use std::signer;
    use std::string::{Self, String};
    use std::vector;
    use std::table::{Self, Table};
    use std::option::{Self, Option};
    use aptos_std::simple_map::{Self, SimpleMap};
    use aptos_std::timestamp;
    use aptos_framework::account;
    use aptos_framework::event::{Self, EventHandle};
    use aptos_framework::table::{Self, TableWithLength};
    use aptos_framework::aptos_coin::AptosCoin;
    use aptos_framework::coin::{Self, Coin};
    use aptos_framework::aptos_coin;

    // Error codes
    const E_NOT_INITIALIZED: u64 = 1;
    const E_ALREADY_INITIALIZED: u64 = 2;
    const E_NOT_AUTHORIZED: u64 = 3;
    const E_AVATAR_NOT_FOUND: u64 = 4;
    const E_HOLON_NOT_FOUND: u64 = 5;
    const E_INVALID_AMOUNT: u64 = 6;
    const E_INSUFFICIENT_BALANCE: u64 = 7;

    // Avatar structure
    struct Avatar has store, copy, drop {
        id: String,
        username: String,
        email: String,
        first_name: String,
        last_name: String,
        avatar_type: String,
        created_date: u64,
        modified_date: u64,
        metadata: String,
        provider_wallets: SimpleMap<String, String>, // provider_type -> wallet_address
    }

    // AvatarDetail structure
    struct AvatarDetail has store, copy, drop {
        id: String,
        username: String,
        email: String,
        first_name: String,
        last_name: String,
        avatar_type: String,
        created_date: u64,
        modified_date: u64,
        metadata: String,
        karma_akashic_records: String,
        xp: u64,
        description: String,
    }

    // Holon structure
    struct Holon has store, copy, drop {
        id: String,
        name: String,
        description: String,
        holon_type: String,
        parent_id: String,
        created_date: u64,
        modified_date: u64,
        metadata: String,
        version: u64,
    }

    // NFT structure
    struct NFT has store, copy, drop {
        token_id: u64,
        owner: address,
        metadata_uri: String,
        name: String,
        description: String,
        created_date: u64,
        attributes: SimpleMap<String, String>,
    }

    // Transaction structure
    struct Transaction has store, copy, drop {
        id: String,
        from_address: String,
        to_address: String,
        amount: u64,
        memo_text: String,
        created_date: u64,
        transaction_hash: String,
    }

    // Storage structures
    struct OasisStorage has key {
        avatars: TableWithLength<String, Avatar>,
        avatar_details: TableWithLength<String, AvatarDetail>,
        holons: TableWithLength<String, Holon>,
        nfts: TableWithLength<u64, NFT>,
        transactions: TableWithLength<String, Transaction>,
        avatar_count: u64,
        holon_count: u64,
        nft_count: u64,
        transaction_count: u64,
    }

    // Events
    struct AvatarCreatedEvent has store, drop {
        avatar_id: String,
        username: String,
        created_by: address,
    }

    struct AvatarUpdatedEvent has store, drop {
        avatar_id: String,
        username: String,
        updated_by: address,
    }

    struct AvatarDeletedEvent has store, drop {
        avatar_id: String,
        username: String,
        deleted_by: address,
    }

    struct HolonCreatedEvent has store, drop {
        holon_id: String,
        name: String,
        created_by: address,
    }

    struct NFTMintedEvent has store, drop {
        token_id: u64,
        owner: address,
        metadata_uri: String,
    }

    struct NFTTransferredEvent has store, drop {
        token_id: u64,
        from: address,
        to: address,
    }

    struct TransactionEvent has store, drop {
        transaction_id: String,
        from_address: String,
        to_address: String,
        amount: u64,
    }

    // Initialize the Oasis storage
    public entry fun initialize(account: &signer) {
        let account_addr = signer::address_of(account);
        
        assert!(!exists<OasisStorage>(account_addr), E_ALREADY_INITIALIZED);
        
        move_to(account, OasisStorage {
            avatars: table::new(),
            avatar_details: table::new(),
            holons: table::new(),
            nfts: table::new(),
            transactions: table::new(),
            avatar_count: 0,
            holon_count: 0,
            nft_count: 0,
            transaction_count: 0,
        });
    }

    // Avatar CRUD operations
    public entry fun create_avatar(
        account: &signer,
        avatar_id: String,
        username: String,
        email: String,
        first_name: String,
        last_name: String,
        avatar_type: String,
        metadata: String
    ) acquires OasisStorage {
        let account_addr = signer::address_of(account);
        let storage = borrow_global_mut<OasisStorage>(account_addr);
        
        let current_time = timestamp::now_seconds();
        
        let avatar = Avatar {
            id: avatar_id,
            username,
            email,
            first_name,
            last_name,
            avatar_type,
            created_date: current_time,
            modified_date: current_time,
            metadata,
            provider_wallets: simple_map::create(),
        };
        
        table::add(&mut storage.avatars, avatar_id, avatar);
        storage.avatar_count = storage.avatar_count + 1;
        
        event::emit(AvatarCreatedEvent {
            avatar_id,
            username: username,
            created_by: account_addr,
        });
    }

    public entry fun update_avatar(
        account: &signer,
        avatar_id: String,
        username: String,
        email: String,
        first_name: String,
        last_name: String,
        avatar_type: String,
        metadata: String
    ) acquires OasisStorage {
        let account_addr = signer::address_of(account);
        let storage = borrow_global_mut<OasisStorage>(account_addr);
        
        assert!(table::contains(&storage.avatars, avatar_id), E_AVATAR_NOT_FOUND);
        
        let mut avatar = table::borrow_mut(&mut storage.avatars, avatar_id);
        avatar.username = username;
        avatar.email = email;
        avatar.first_name = first_name;
        avatar.last_name = last_name;
        avatar.avatar_type = avatar_type;
        avatar.metadata = metadata;
        avatar.modified_date = timestamp::now_seconds();
        
        event::emit(AvatarUpdatedEvent {
            avatar_id,
            username: username,
            updated_by: account_addr,
        });
    }

    public entry fun delete_avatar(
        account: &signer,
        avatar_id: String
    ) acquires OasisStorage {
        let account_addr = signer::address_of(account);
        let storage = borrow_global_mut<OasisStorage>(account_addr);
        
        assert!(table::contains(&storage.avatars, avatar_id), E_AVATAR_NOT_FOUND);
        
        let avatar = table::remove(&mut storage.avatars, avatar_id);
        storage.avatar_count = storage.avatar_count - 1;
        
        event::emit(AvatarDeletedEvent {
            avatar_id,
            username: avatar.username,
            deleted_by: account_addr,
        });
    }

    // AvatarDetail CRUD operations
    public entry fun create_avatar_detail(
        account: &signer,
        avatar_id: String,
        username: String,
        email: String,
        first_name: String,
        last_name: String,
        avatar_type: String,
        metadata: String,
        karma_akashic_records: String,
        xp: u64,
        description: String
    ) acquires OasisStorage {
        let account_addr = signer::address_of(account);
        let storage = borrow_global_mut<OasisStorage>(account_addr);
        
        let current_time = timestamp::now_seconds();
        
        let avatar_detail = AvatarDetail {
            id: avatar_id,
            username,
            email,
            first_name,
            last_name,
            avatar_type,
            created_date: current_time,
            modified_date: current_time,
            metadata,
            karma_akashic_records,
            xp,
            description,
        };
        
        table::add(&mut storage.avatar_details, avatar_id, avatar_detail);
    }

    // Holon CRUD operations
    public entry fun create_holon(
        account: &signer,
        holon_id: String,
        name: String,
        description: String,
        holon_type: String,
        parent_id: String,
        metadata: String
    ) acquires OasisStorage {
        let account_addr = signer::address_of(account);
        let storage = borrow_global_mut<OasisStorage>(account_addr);
        
        let current_time = timestamp::now_seconds();
        
        let holon = Holon {
            id: holon_id,
            name,
            description,
            holon_type,
            parent_id,
            created_date: current_time,
            modified_date: current_time,
            metadata,
            version: 1,
        };
        
        table::add(&mut storage.holons, holon_id, holon);
        storage.holon_count = storage.holon_count + 1;
        
        event::emit(HolonCreatedEvent {
            holon_id,
            name: name,
            created_by: account_addr,
        });
    }

    // NFT operations
    public entry fun mint_nft(
        account: &signer,
        to: address,
        metadata_uri: String,
        name: String,
        description: String,
        attributes: SimpleMap<String, String>
    ) acquires OasisStorage {
        let account_addr = signer::address_of(account);
        let storage = borrow_global_mut<OasisStorage>(account_addr);
        
        let token_id = storage.nft_count + 1;
        let current_time = timestamp::now_seconds();
        
        let nft = NFT {
            token_id,
            owner: to,
            metadata_uri,
            name,
            description,
            created_date: current_time,
            attributes,
        };
        
        table::add(&mut storage.nfts, token_id, nft);
        storage.nft_count = storage.nft_count + 1;
        
        event::emit(NFTMintedEvent {
            token_id,
            owner: to,
            metadata_uri,
        });
    }

    public entry fun transfer_nft(
        account: &signer,
        token_id: u64,
        to: address
    ) acquires OasisStorage {
        let account_addr = signer::address_of(account);
        let storage = borrow_global_mut<OasisStorage>(account_addr);
        
        assert!(table::contains(&storage.nfts, token_id), E_HOLON_NOT_FOUND);
        
        let mut nft = table::borrow_mut(&mut storage.nfts, token_id);
        let from = nft.owner;
        nft.owner = to;
        
        event::emit(NFTTransferredEvent {
            token_id,
            from,
            to,
        });
    }

    // Transaction operations
    public entry fun send_transaction(
        account: &signer,
        to_address: String,
        amount: u64,
        memo_text: String
    ) acquires OasisStorage {
        let account_addr = signer::address_of(account);
        let storage = borrow_global_mut<OasisStorage>(account_addr);
        
        let transaction_id = string::utf8(b"tx_") + string::utf8(b"123456789"); // Simplified ID generation
        let current_time = timestamp::now_seconds();
        
        let transaction = Transaction {
            id: transaction_id,
            from_address: string::utf8(b"0x1"), // Simplified
            to_address,
            amount,
            memo_text,
            created_date: current_time,
            transaction_hash: string::utf8(b"0xhash"), // Simplified
        };
        
        table::add(&mut storage.transactions, transaction_id, transaction);
        storage.transaction_count = storage.transaction_count + 1;
        
        event::emit(TransactionEvent {
            transaction_id,
            from_address: string::utf8(b"0x1"),
            to_address,
            amount,
        });
    }

    // View functions
    public fun get_avatar(account_addr: address, avatar_id: String): Option<Avatar> acquires OasisStorage {
        let storage = borrow_global<OasisStorage>(account_addr);
        if (table::contains(&storage.avatars, avatar_id)) {
            option::some(*table::borrow(&storage.avatars, avatar_id))
        } else {
            option::none()
        }
    }

    public fun get_avatar_count(account_addr: address): u64 acquires OasisStorage {
        let storage = borrow_global<OasisStorage>(account_addr);
        storage.avatar_count
    }

    public fun get_holon_count(account_addr: address): u64 acquires OasisStorage {
        let storage = borrow_global<OasisStorage>(account_addr);
        storage.holon_count
    }

    public fun get_nft_count(account_addr: address): u64 acquires OasisStorage {
        let storage = borrow_global<OasisStorage>(account_addr);
        storage.nft_count
    }
}

