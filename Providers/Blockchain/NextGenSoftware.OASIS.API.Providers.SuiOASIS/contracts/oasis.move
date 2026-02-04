module oasis::oasis {
    use sui::object::{Self, UID, ID};
    use sui::tx_context::{Self, TxContext};
    use sui::table::{Self, Table};
    use sui::event;
    use sui::transfer;
    use std::string::{Self, String};
    use std::vector;
    use std::option::{Self, Option};

    /// Avatar object stored on-chain
    struct Avatar has key, store {
        id: UID,
        avatar_id: String,
        username: String,
        email: String,
        first_name: String,
        last_name: String,
        created_at: u64,
        modified_at: u64,
    }

    /// AvatarDetail object stored on-chain
    struct AvatarDetail has key, store {
        id: UID,
        avatar_id: String,
        username: String,
        email: String,
        karma_akashic_records: String,
        xp: u64,
        level: u64,
        created_at: u64,
        modified_at: u64,
    }

    /// Holon object stored on-chain
    struct Holon has key, store {
        id: UID,
        holon_id: String,
        name: String,
        description: String,
        parent_id: String,
        holon_type: u8,
        created_at: u64,
        modified_at: u64,
    }

    /// Main OASIS storage object
    struct OasisStorage has key {
        id: UID,
        avatars: Table<String, ID>, // username -> Avatar object ID
        avatars_by_email: Table<String, ID>, // email -> Avatar object ID
        avatar_details: Table<String, ID>, // username -> AvatarDetail object ID
        holons: Table<String, ID>, // holon_id -> Holon object ID
        holons_by_parent: Table<String, vector<ID>>, // parent_id -> vector of Holon object IDs
        holons_by_metadata: Table<String, vector<ID>>, // metadata_key -> vector of Holon object IDs
    }

    /// Events
    struct AvatarCreated has copy, drop {
        avatar_id: String,
        username: String,
    }

    struct AvatarDetailCreated has copy, drop {
        avatar_id: String,
        username: String,
    }

    struct HolonCreated has copy, drop {
        holon_id: String,
        name: String,
        parent_id: String,
    }

    /// Initialize OASIS storage
    public fun init(ctx: &mut TxContext) {
        let storage = OasisStorage {
            id: object::new(ctx),
            avatars: table::new(ctx),
            avatars_by_email: table::new(ctx),
            avatar_details: table::new(ctx),
            holons: table::new(ctx),
            holons_by_parent: table::new(ctx),
            holons_by_metadata: table::new(ctx),
        };
        transfer::share_object(storage);
    }

    /// Create avatar entry function
    public entry fun create_avatar(
        storage: &mut OasisStorage,
        avatar_id: vector<u8>,
        username: vector<u8>,
        email: vector<u8>,
        first_name: vector<u8>,
        last_name: vector<u8>,
        ctx: &mut TxContext
    ) {
        let avatar_id_str = string::utf8(avatar_id);
        let username_str = string::utf8(username);
        let email_str = string::utf8(email);
        let first_name_str = string::utf8(first_name);
        let last_name_str = string::utf8(last_name);

        let avatar = Avatar {
            id: object::new(ctx),
            avatar_id: avatar_id_str,
            username: username_str,
            email: email_str,
            first_name: first_name_str,
            last_name: last_name_str,
            created_at: tx_context::epoch_timestamp_ms(ctx),
            modified_at: tx_context::epoch_timestamp_ms(ctx),
        };

        let avatar_id_obj = object::id(&avatar);
        table::add(&mut storage.avatars, username_str, avatar_id_obj);
        table::add(&mut storage.avatars_by_email, email_str, avatar_id_obj);
        transfer::share_object(avatar);

        event::emit(AvatarCreated {
            avatar_id: avatar_id_str,
            username: username_str,
        });
    }

    /// Save avatar detail entry function
    public entry fun save_avatar_detail(
        storage: &mut OasisStorage,
        avatar_id: vector<u8>,
        username: vector<u8>,
        email: vector<u8>,
        karma_akashic_records: vector<u8>,
        xp: u64,
        level: u64,
        ctx: &mut TxContext
    ) {
        let avatar_id_str = string::utf8(avatar_id);
        let username_str = string::utf8(username);
        let email_str = string::utf8(email);
        let karma_str = string::utf8(karma_akashic_records);

        let avatar_detail = AvatarDetail {
            id: object::new(ctx),
            avatar_id: avatar_id_str,
            username: username_str,
            email: email_str,
            karma_akashic_records: karma_str,
            xp,
            level,
            created_at: tx_context::epoch_timestamp_ms(ctx),
            modified_at: tx_context::epoch_timestamp_ms(ctx),
        };

        let detail_id = object::id(&avatar_detail);
        table::add(&mut storage.avatar_details, username_str, detail_id);
        transfer::share_object(avatar_detail);

        event::emit(AvatarDetailCreated {
            avatar_id: avatar_id_str,
            username: username_str,
        });
    }

    /// Save holon entry function
    public entry fun save_holon(
        storage: &mut OasisStorage,
        holon_id: vector<u8>,
        name: vector<u8>,
        description: vector<u8>,
        parent_id: vector<u8>,
        holon_type: u8,
        ctx: &mut TxContext
    ) {
        let holon_id_str = string::utf8(holon_id);
        let name_str = string::utf8(name);
        let description_str = string::utf8(description);
        let parent_id_str = string::utf8(parent_id);

        let holon = Holon {
            id: object::new(ctx),
            holon_id: holon_id_str,
            name: name_str,
            description: description_str,
            parent_id: parent_id_str,
            holon_type,
            created_at: tx_context::epoch_timestamp_ms(ctx),
            modified_at: tx_context::epoch_timestamp_ms(ctx),
        };

        let holon_id_obj = object::id(&holon);
        table::add(&mut storage.holons, holon_id_str, holon_id_obj);

        // Add to parent index
        if (!table::contains(&storage.holons_by_parent, parent_id_str)) {
            table::add(&mut storage.holons_by_parent, parent_id_str, vector::empty());
        };
        let mut parent_holons = table::borrow_mut(&mut storage.holons_by_parent, parent_id_str);
        vector::push_back(parent_holons, holon_id_obj);

        transfer::share_object(holon);

        event::emit(HolonCreated {
            holon_id: holon_id_str,
            name: name_str,
            parent_id: parent_id_str,
        });
    }

    /// View function: Get avatar by username
    public fun get_avatar_by_username(storage: &OasisStorage, username: vector<u8>): Option<ID> {
        let username_str = string::utf8(username);
        if (table::contains(&storage.avatars, username_str)) {
            option::some(*table::borrow(&storage.avatars, username_str))
        } else {
            option::none()
        }
    }

    /// View function: Get avatar by email
    public fun get_avatar_by_email(storage: &OasisStorage, email: vector<u8>): Option<ID> {
        let email_str = string::utf8(email);
        if (table::contains(&storage.avatars_by_email, email_str)) {
            option::some(*table::borrow(&storage.avatars_by_email, email_str))
        } else {
            option::none()
        }
    }

    /// View function: Get holons by parent
    public fun get_holons_by_parent(storage: &OasisStorage, parent_id: vector<u8>): vector<ID> {
        let parent_id_str = string::utf8(parent_id);
        if (table::contains(&storage.holons_by_parent, parent_id_str)) {
            *table::borrow(&storage.holons_by_parent, parent_id_str)
        } else {
            vector::empty()
        }
    }

    /// View function: Get holon by ID
    public fun get_holon_by_id(storage: &OasisStorage, holon_id: vector<u8>): Option<ID> {
        let holon_id_str = string::utf8(holon_id);
        if (table::contains(&storage.holons, holon_id_str)) {
            option::some(*table::borrow(&storage.holons, holon_id_str))
        } else {
            option::none()
        }
    }

    /// View function: Search holons
    public fun search_holons(storage: &OasisStorage, query: vector<u8>): vector<ID> {
        // Simplified search - in production would iterate through holons and match
        vector::empty()
    }
}

