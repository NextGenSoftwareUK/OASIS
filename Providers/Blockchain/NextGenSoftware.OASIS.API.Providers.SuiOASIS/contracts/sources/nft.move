module oasis::nft;

use std::string::String;

// Address-owned, transferable Sui objects; ownership is enforced by the VM.
public struct NFT has key, store {
    id: UID,
    creator: address,
    metadata: String,
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
