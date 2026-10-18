module oasis::token;

use sui::coin::{Self, Coin, TreasuryCap};
use sui::coin_registry;

const ENotIssuer: u64 = 1;
const ENotOwner: u64 = 2;
const EZeroAmount: u64 = 3;

public struct TOKEN has drop {}

// Standard Sui coins, with issuer-controlled minting and holder-authorized burns.
public struct Currency has key {
    id: UID,
    issuer: address,
    treasury: TreasuryCap<TOKEN>,
}

// No `store` ability: holders cannot bypass the module's unlock policy.
public struct LockedCoin has key {
    id: UID,
    owner: address,
    coin: Coin<TOKEN>,
}

fun init(witness: TOKEN, ctx: &mut TxContext) {
    let (metadata, treasury) = coin_registry::new_currency_with_otw(
        witness, 9, std::string::utf8(b"OASIS"), std::string::utf8(b"OASIS"),
        std::string::utf8(b"OASIS transferable Sui coin"), std::string::utf8(b""), ctx,
    );
    coin_registry::finalize_and_delete_metadata_cap(metadata, ctx);
    transfer::share_object(Currency { id: object::new(ctx), issuer: ctx.sender(), treasury });
}

public fun mint(currency: &mut Currency, amount: u64, ctx: &mut TxContext): Coin<TOKEN> {
    assert!(currency.issuer == ctx.sender(), ENotIssuer);
    assert!(amount > 0, EZeroAmount);
    coin::mint(&mut currency.treasury, amount, ctx)
}

public fun burn(currency: &mut Currency, coin: Coin<TOKEN>) {
    coin::burn(&mut currency.treasury, coin);
}

public fun lock(coin: Coin<TOKEN>, ctx: &mut TxContext) {
    assert!(coin::value(&coin) > 0, EZeroAmount);
    let locked = LockedCoin { id: object::new(ctx), owner: ctx.sender(), coin };
    transfer::transfer(locked, ctx.sender());
}

public fun unlock(locked: LockedCoin, ctx: &TxContext): Coin<TOKEN> {
    let LockedCoin { id, owner, coin } = locked;
    assert!(owner == ctx.sender(), ENotOwner);
    object::delete(id);
    coin
}

#[test]
fun issuer_mint_and_holder_burn_update_real_supply() {
    let mut ctx = tx_context::dummy();
    let mut currency = Currency { id: object::new(&mut ctx), issuer: ctx.sender(), treasury: coin::create_treasury_cap_for_testing(&mut ctx) };
    let coin = mint(&mut currency, 123, &mut ctx);
    assert!(coin::value(&coin) == 123, 0);
    assert!(coin::total_supply(&currency.treasury) == 123, 0);
    burn(&mut currency, coin);
    assert!(coin::total_supply(&currency.treasury) == 0, 0);
    transfer::share_object(currency);
}

#[test]
#[expected_failure(abort_code = ENotIssuer)]
fun unauthorized_mint_aborts() {
    let mut ctx = tx_context::dummy();
    let mut currency = Currency { id: object::new(&mut ctx), issuer: @0x123, treasury: coin::create_treasury_cap_for_testing(&mut ctx) };
    let _coin = mint(&mut currency, 1, &mut ctx);
    abort 0
}

#[test]
fun owner_unlock_returns_the_exact_locked_coin() {
    let mut ctx = tx_context::dummy();
    let coin = coin::mint_for_testing<TOKEN>(456, &mut ctx);
    let locked = LockedCoin { id: object::new(&mut ctx), owner: ctx.sender(), coin };
    let restored = unlock(locked, &ctx);
    assert!(coin::value(&restored) == 456, 0);
    coin::burn_for_testing(restored);
}

#[test]
#[expected_failure(abort_code = ENotOwner)]
fun unauthorized_unlock_aborts() {
    let mut ctx = tx_context::dummy();
    let coin = coin::mint_for_testing<TOKEN>(1, &mut ctx);
    let locked = LockedCoin { id: object::new(&mut ctx), owner: @0x123, coin };
    let _restored = unlock(locked, &ctx);
    abort 0
}
