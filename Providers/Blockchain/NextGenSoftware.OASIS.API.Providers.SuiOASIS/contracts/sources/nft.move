module oasis::nft;

use std::string::String;

// Address-owned, transferable Sui objects; ownership is enforced by the VM.
public struct NFT has key, store {
    id: UID,
    creator: address,
    metadata: String,
}

const E_NOT_OWNER: u64 = 1;

// No store ability: the escrow cannot be publicly transferred or unpacked.
public struct LockedNFT has key {
    id: UID,
    owner: address,
    nft: NFT,
}

public fun lock(nft: NFT, ctx: &mut TxContext) {
    let owner = ctx.sender();
    transfer::transfer(LockedNFT { id: object::new(ctx), owner, nft }, owner);
}

public fun unlock(locked: LockedNFT, ctx: &TxContext): NFT {
    assert!(locked.owner == ctx.sender(), E_NOT_OWNER);
    let LockedNFT { id, owner: _, nft } = locked;
    object::delete(id);
    nft
}

public fun mint(metadata: String, recipient: address, ctx: &mut TxContext) {
    let nft = NFT { id: object::new(ctx), creator: ctx.sender(), metadata };
    transfer::public_transfer(nft, recipient);
}

public fun burn(nft: NFT) {
    let NFT { id, creator: _, metadata: _ } = nft;
    object::delete(id);
}

#[test]
fun object_metadata_and_burn() {
    let mut ctx = tx_context::dummy();
    let nft = NFT { id: object::new(&mut ctx), creator: ctx.sender(), metadata: std::string::utf8(b"complete metadata") };
    assert!(nft.metadata == std::string::utf8(b"complete metadata"), 0);
    burn(nft);
}

#[test]
fun escrow_preserves_original_nft() {
    let mut ctx = tx_context::dummy();
    let nft = NFT { id: object::new(&mut ctx), creator: ctx.sender(), metadata: std::string::utf8(b"escrow") };
    let original_id = object::id(&nft);
    let locked = LockedNFT { id: object::new(&mut ctx), owner: ctx.sender(), nft };
    let nft = unlock(locked, &ctx);
    assert!(object::id(&nft) == original_id, 0);
    burn(nft);
}

#[test, expected_failure(abort_code = E_NOT_OWNER)]
fun escrow_rejects_wrong_owner() {
    let mut ctx = tx_context::dummy();
    let nft = NFT { id: object::new(&mut ctx), creator: ctx.sender(), metadata: std::string::utf8(b"escrow") };
    let locked = LockedNFT { id: object::new(&mut ctx), owner: @0x123, nft };
    burn(unlock(locked, &ctx));
}
