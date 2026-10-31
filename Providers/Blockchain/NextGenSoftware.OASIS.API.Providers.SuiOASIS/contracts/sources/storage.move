module oasis::storage;

use std::string::String;
use sui::table::{Self, Table};

const ENotOwner: u64 = 1;

public struct Storage has key {
    id: UID,
    owner: address,
    values: Table<String, String>,
}

fun init(ctx: &mut TxContext) {
    let storage = Storage {
        id: object::new(ctx),
        owner: ctx.sender(),
        values: table::new(ctx),
    };
    transfer::share_object(storage);
}

public fun put(storage: &mut Storage, key: String, value: String, ctx: &TxContext) {
    assert!(storage.owner == ctx.sender(), ENotOwner);
    if (table::contains(&storage.values, key)) {
        *table::borrow_mut(&mut storage.values, key) = value;
    } else {
        table::add(&mut storage.values, key, value);
    };
}

public fun remove(storage: &mut Storage, key: String, ctx: &TxContext) {
    assert!(storage.owner == ctx.sender(), ENotOwner);
    if (table::contains(&storage.values, key)) {
        let _ = table::remove(&mut storage.values, key);
    };
}

#[test]
fun owner_writes_update_and_idempotent_delete() {
    let mut ctx = tx_context::dummy();
    let mut storage = Storage { id: object::new(&mut ctx), owner: ctx.sender(), values: table::new(&mut ctx) };
    let key = std::string::utf8(b"oasis/test");
    put(&mut storage, key, std::string::utf8(b"one"), &ctx);
    put(&mut storage, key, std::string::utf8(b"two"), &ctx);
    assert!(*table::borrow(&storage.values, key) == std::string::utf8(b"two"), 0);
    remove(&mut storage, key, &ctx);
    remove(&mut storage, key, &ctx);
    let Storage { id, owner: _, values } = storage;
    table::destroy_empty(values);
    object::delete(id);
}

#[test]
#[expected_failure(abort_code = ENotOwner)]
fun unauthorized_write_aborts() {
    let mut ctx = tx_context::dummy();
    let mut storage = Storage { id: object::new(&mut ctx), owner: @0x123, values: table::new(&mut ctx) };
    put(&mut storage, std::string::utf8(b"key"), std::string::utf8(b"forbidden"), &ctx);
    abort 0
}
