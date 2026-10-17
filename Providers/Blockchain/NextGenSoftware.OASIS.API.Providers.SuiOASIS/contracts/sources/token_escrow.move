module oasis::token_escrow;

use oasis::token::TOKEN;
use sui::balance::{Self, Balance};
use sui::coin::{Self, Coin};
use sui::table::{Self, Table};

const EInvalidId: u64 = 1;
const EZeroAmount: u64 = 2;
const EUsedId: u64 = 3;
const ENotOwner: u64 = 4;
const EReleased: u64 = 5;
const EAmountMismatch: u64 = 6;

// Shared custody registry, distinct from token::LockedCoin's aggregate ABI.
// IDs are SHA256 bytes of the canonical original OASIS GUID, never regenerated.
public struct Escrow has key {
    id: UID,
    protocol: u64,
    deposits: Table<vector<u8>, Deposit>,
}

// No removal API: a released operation ID stays occupied permanently.
public struct Deposit has store {
    owner: address,
    amount: u64,
    released: bool,
    funds: Balance<TOKEN>,
}

public fun create(ctx: &mut TxContext) {
    transfer::share_object(new_escrow(ctx));
}

fun new_escrow(ctx: &mut TxContext): Escrow {
    Escrow { id: object::new(ctx), protocol: 1, deposits: table::new(ctx) }
}

public fun lock(escrow: &mut Escrow, deposit_id: vector<u8>, coin: Coin<TOKEN>, ctx: &TxContext) {
    assert!(deposit_id.length() == 32, EInvalidId);
    let amount = coin::value(&coin);
    assert!(amount > 0, EZeroAmount);
    assert!(!escrow.deposits.contains(deposit_id), EUsedId);
    escrow.deposits.add(deposit_id, Deposit {
        owner: ctx.sender(), amount, released: false, funds: coin::into_balance(coin),
    });
}

public fun release(escrow: &mut Escrow, deposit_id: vector<u8>, expected_amount: u64, ctx: &mut TxContext): Coin<TOKEN> {
    assert!(deposit_id.length() == 32, EInvalidId);
    let deposit = escrow.deposits.borrow_mut(deposit_id);
    assert!(deposit.owner == ctx.sender(), ENotOwner);
    assert!(!deposit.released, EReleased);
    assert!(deposit.amount == expected_amount && balance::value(&deposit.funds) == expected_amount, EAmountMismatch);
    deposit.released = true;
    coin::from_balance(balance::withdraw_all(&mut deposit.funds), ctx)
}

#[test]
fun independent_deposits_return_exact_coins_and_keep_used_ids() {
    let mut ctx = tx_context::dummy();
    let mut escrow = new_escrow(&mut ctx);
    let first = b"11111111111111111111111111111111";
    let second = b"22222222222222222222222222222222";
    lock(&mut escrow, first, coin::mint_for_testing<TOKEN>(123, &mut ctx), &ctx);
    lock(&mut escrow, second, coin::mint_for_testing<TOKEN>(456, &mut ctx), &ctx);
    let restored = release(&mut escrow, first, 123, &mut ctx);
    assert!(coin::value(&restored) == 123, 0);
    coin::burn_for_testing(restored);
    let original = escrow.deposits.borrow(first);
    assert!(original.released && original.amount == 123 && balance::value(&original.funds) == 0, 0);
    let other = escrow.deposits.borrow(second);
    assert!(!other.released && balance::value(&other.funds) == 456, 0);
    assert!(escrow.protocol == 1, 0);
    transfer::share_object(escrow);
}

#[test]
fun maximum_u64_coin_roundtrips_without_precision_loss() {
    let mut ctx = tx_context::dummy(); let mut escrow = new_escrow(&mut ctx);
    let id = b"11111111111111111111111111111111";
    lock(&mut escrow, id, coin::mint_for_testing<TOKEN>(18446744073709551615, &mut ctx), &ctx);
    let restored = release(&mut escrow, id, 18446744073709551615, &mut ctx);
    assert!(coin::value(&restored) == 18446744073709551615, 0);
    coin::burn_for_testing(restored); transfer::share_object(escrow);
}

#[test]
#[expected_failure(abort_code = EInvalidId)]
fun malformed_id_aborts() {
    let mut ctx = tx_context::dummy(); let mut escrow = new_escrow(&mut ctx);
    lock(&mut escrow, b"short", coin::mint_for_testing<TOKEN>(1, &mut ctx), &ctx);
    abort 0
}

#[test]
#[expected_failure(abort_code = EZeroAmount)]
fun zero_coin_aborts() {
    let mut ctx = tx_context::dummy(); let mut escrow = new_escrow(&mut ctx);
    lock(&mut escrow, b"11111111111111111111111111111111", coin::mint_for_testing<TOKEN>(0, &mut ctx), &ctx);
    abort 0
}

#[test]
#[expected_failure(abort_code = EUsedId)]
fun duplicate_id_aborts() {
    let mut ctx = tx_context::dummy(); let mut escrow = new_escrow(&mut ctx);
    let id = b"11111111111111111111111111111111";
    lock(&mut escrow, id, coin::mint_for_testing<TOKEN>(1, &mut ctx), &ctx);
    lock(&mut escrow, id, coin::mint_for_testing<TOKEN>(1, &mut ctx), &ctx);
    abort 0
}

#[test]
#[expected_failure(abort_code = ENotOwner)]
fun foreign_owner_cannot_release() {
    let mut ctx = tx_context::dummy(); let mut escrow = new_escrow(&mut ctx);
    let id = b"11111111111111111111111111111111";
    lock(&mut escrow, id, coin::mint_for_testing<TOKEN>(1, &mut ctx), &ctx);
    escrow.deposits.borrow_mut(id).owner = @0x123;
    let _coin = release(&mut escrow, id, 1, &mut ctx); abort 0
}

#[test]
#[expected_failure(abort_code = EAmountMismatch)]
fun wrong_amount_cannot_release() {
    let mut ctx = tx_context::dummy(); let mut escrow = new_escrow(&mut ctx);
    let id = b"11111111111111111111111111111111";
    lock(&mut escrow, id, coin::mint_for_testing<TOKEN>(2, &mut ctx), &ctx);
    let _coin = release(&mut escrow, id, 1, &mut ctx); abort 0
}

#[test]
#[expected_failure(abort_code = EReleased)]
fun released_id_cannot_release_again() {
    let mut ctx = tx_context::dummy(); let mut escrow = new_escrow(&mut ctx);
    let id = b"11111111111111111111111111111111";
    lock(&mut escrow, id, coin::mint_for_testing<TOKEN>(1, &mut ctx), &ctx);
    coin::burn_for_testing(release(&mut escrow, id, 1, &mut ctx));
    let _coin = release(&mut escrow, id, 1, &mut ctx); abort 0
}

#[test]
#[expected_failure(abort_code = EUsedId)]
fun released_id_cannot_be_redeposited() {
    let mut ctx = tx_context::dummy(); let mut escrow = new_escrow(&mut ctx);
    let id = b"11111111111111111111111111111111";
    lock(&mut escrow, id, coin::mint_for_testing<TOKEN>(1, &mut ctx), &ctx);
    coin::burn_for_testing(release(&mut escrow, id, 1, &mut ctx));
    lock(&mut escrow, id, coin::mint_for_testing<TOKEN>(1, &mut ctx), &ctx); abort 0
}
